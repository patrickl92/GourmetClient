using System.Globalization;
using System.Net.Http;

namespace GourmetClient.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using GourmetClient.Model;
    using GourmetClient.Utils;
    using HtmlAgilityPack;

    public class GourmetWebClient : WebClientBase
    {
        private const string WebUrl = "https://bestellung.gourmetcatering.at/frontend4/default.aspx";

        private const string PageNameLogin = "Login";

        private const string PageNameLogout = "Logout";

        private const string PageNameUserSettings = "UserSettings";

        private const string PageNameMenu = "MenuCalendar";

        private const string PageNameOrderedMenu = "ShoppingCart";

        private const string PageNameAddMealToOrderedMenu = "AddItem";

        private const string PageNameBilling = "Billing";

        private const string ActionNameAddMealToOrderedMenu = "AddItemToShoppingCart";

        private const string ActionNameCancelMealOrder = "CancelItem";

        protected override async Task<bool> LoginImpl(string userName, SecureString password)
        {
            var parameters = new Dictionary<string, string>
            {
                {"login", userName},
                {"password", password.ToPlainPassword()},
                {"btnSubmit", "1"}
            };
            
            using var response = await ExecutePostRequest(WebUrl, GetUrlParametersForPage(PageNameLogin), parameters);
            var httpContent = await GetResponseContent(response);

            // Login is successful if redirect to menu calendar is received
            return Regex.IsMatch(httpContent, "<meta\\s+http-equiv=\"refresh\"\\s+content=\"\\d+;\\s*URL=default\\.aspx\\?c=MenuCalendar\">");
        }

        protected override async Task LogoutImpl()
        {
            using var response = await ExecuteGetRequestForPage(PageNameLogout);
        }

        public async Task<GourmetUserData> GetUserData()
        {
            using var response = await ExecuteGetRequestForPage(PageNameUserSettings);
            var httpContent = await GetResponseContent(response);

            if (!Regex.IsMatch(httpContent, "<div\\s+class=\"settings.*\">"))
            {
                return null;
            }

            var nameOfUser = ParseHtmlForNameOfUser(httpContent);

            if (nameOfUser == null)
            {
                return null;
            }

            return new GourmetUserData(nameOfUser);
        }

        public async Task<GourmetMenu> GetMenu()
        {
            using var response = await ExecuteGetRequestForPage(PageNameMenu);
            var httpContent = await GetResponseContent(response);

            try
            {
                return ParseGourmetMenuHtml(httpContent);
            }
            catch (Exception exception)
            {
                throw new GourmetParseException("Error parsing the menu HTML", GetRequestUriString(response), httpContent, exception);
            }
        }

        public async Task<OrderedGourmetMenu> GetOrderedMenu()
        {
            using var response = await ExecuteGetRequestForPage(PageNameOrderedMenu);
            var httpContent = await GetResponseContent(response);

            try
            {
                return ParseOrderedGourmetMenuHtml(httpContent);
            }
            catch (Exception exception)
            {
                throw new GourmetParseException("Error parsing the ordered menu HTML", GetRequestUriString(response), httpContent, exception);
            }
        }

        public async Task AddMealToOrderedMenu(GourmetMenuMeal meal)
        {
            meal = meal ?? throw new ArgumentNullException(nameof(meal));

            if (string.IsNullOrEmpty(meal.ProductId))
            {
                throw new InvalidOperationException($"Meal {meal.Name} (Description: '{meal.Description}') cannot be ordered");
            }

            var parameters = new Dictionary<string, string>
            {
                { "ProductID", meal.ProductId },
                { "IsMenu", "1" }
            };
            
            using var response = await ExecuteGetRequest(WebUrl, GetUrlParametersForPage(PageNameAddMealToOrderedMenu, ActionNameAddMealToOrderedMenu, parameters));
        }

        public async Task CancelOrder(OrderedGourmetMenuMeal orderedMeal)
        {
            orderedMeal = orderedMeal ?? throw new ArgumentNullException(nameof(orderedMeal));

            if (!orderedMeal.IsOrderCancelable)
            {
                throw new InvalidOperationException($"Order {orderedMeal.Name} (OrderId: '{orderedMeal.OrderId}') cannot be canceled");
            }

            var parameters = new Dictionary<string, string>
            {
                { "id", orderedMeal.OrderId },
                { "ismenu", "1" }
            };
            
            using var response = await ExecuteGetRequest(WebUrl, GetUrlParametersForPage(PageNameOrderedMenu, ActionNameCancelMealOrder, parameters));
        }

        public async Task ConfirmOrder()
        {
            using var responseOrderedMenu = await ExecuteGetRequestForPage(PageNameOrderedMenu);
            var httpContentOrderedMenu = await GetResponseContent(responseOrderedMenu);
            var confirmParameters = ParseConfirmParametersFromOrderedGourmetMenuHtml(httpContentOrderedMenu);

            using var response = await ExecutePostRequest(WebUrl, null, confirmParameters);
        }

        public async Task<IReadOnlyList<BillingPosition>> GetBillingPositions(int month, int year, IProgress<int> progress)
        {
            var parameters = new Dictionary<string, string>
            {
                {"PostBackSelectMonth", "1"},
                {"inputAbrechnung", $"{month}-{year}"}
            };

            using var response = await ExecutePostRequest(WebUrl, GetUrlParametersForPage(PageNameBilling), parameters);
            var httpContent = await GetResponseContent(response);

            var billingPositions = ParseBillingPositionsFromBillingHtml(httpContent);

            progress.Report(100);

            return billingPositions;
        }

        private Task<HttpResponseMessage> ExecuteGetRequestForPage(string pageName)
        {
            return ExecuteGetRequest(WebUrl, GetUrlParametersForPage(pageName));
        }

        private static string ParseHtmlForNameOfUser(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var loginNameNode = document.DocumentNode.SelectSingleNode("//div[@class='userfield']//span[@class='loginname']");
            if (loginNameNode == null)
            {
                return null;
            }

            return loginNameNode.GetInnerText();
        }

        private static GourmetMenu ParseGourmetMenuHtml(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var days = new List<GourmetMenuDay>();
            var today = DateTimeHelper.GetToday();

            foreach (var dayNode in document.DocumentNode.GetNodes("//div[contains(@class, 'menuitem')]"))
            {
                var headerNode = dayNode.SelectSingleNode(".//div[contains(@class, 'header')]");
                if (headerNode == null)
                {
                    throw new InvalidOperationException("No header node found in HTML");
                }

                var dateNode = headerNode.SelectSingleNode(".//span[@class='date']");
                if (dateNode == null)
                {
                    throw new InvalidOperationException("No date node found in HTML");
                }

                var dateNodeValue = dateNode.GetInnerText();
                var date = GetDateFromMenuDayNodeValue(dateNodeValue);

                if (date < today)
                {
                    // Happens on new year
                    date = date.AddYears(1);
                }

                var meals = ParseGourmetMeals(dayNode);

                days.Add(new GourmetMenuDay(date, meals));
            }

            return new GourmetMenu(days);
        }

        private static IReadOnlyList<GourmetMenuMeal> ParseGourmetMeals(HtmlNode dayNode)
        {
            var meals = new List<GourmetMenuMeal>();

            foreach (var mealNode in dayNode.GetNodes(".//div[contains(@class, 'meal')]"))
            {
                var titleNode = mealNode.GetSingleNode(".//span[@class='title']");
                var subTitleNode = mealNode.GetSingleNode(".//span[@class='subtitle']");
                var orderNode = mealNode.GetSingleNode(".//div[@class='order']");

                if (!orderNode.Attributes.Contains("data-remoteurl"))
                {
                    throw new InvalidOperationException("No remote url attribute found in HTML");
                }

                var title = titleNode.GetInnerText();
                var subTitle = subTitleNode.GetInnerText();
                var remoteUrl = orderNode.Attributes["data-remoteurl"].Value;
                var decodedRemoteUrl = WebUtility.HtmlDecode(remoteUrl) ?? throw new InvalidOperationException($"Remote url '{remoteUrl}' could not be decoded");

                var match = Regex.Match(decodedRemoteUrl, "&ProductID=([0-9]+)&");
                if (!match.Success)
                {
                    throw new InvalidOperationException($"Remote url '{decodedRemoteUrl}' has an invalid format");
                }

                var productId = match.Groups[1].Value;
                var name = title;
                var description = subTitle;

                meals.Add(new GourmetMenuMeal(productId, name, description));
            }

            return meals;
        }

        private static OrderedGourmetMenu ParseOrderedGourmetMenuHtml(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var days = new List<OrderedGourmetMenuDay>();

            foreach (var orderItemNode in document.DocumentNode.GetNodes("//div[contains(@class, 'order-item')]"))
            {
                var dateInputNode = orderItemNode.GetSingleNode(".//input[contains(@name, 'order_') and @type='hidden']");
                var titleNode = orderItemNode.GetSingleNode(".//div[@class='title']");

                var dateInputNameValue = dateInputNode.Attributes["name"].Value;

                var orderIdMatch = Regex.Match(dateInputNameValue, "^order_([0-9]+)$");
                if (!orderIdMatch.Success)
                {
                    throw new InvalidOperationException($"Order id '{dateInputNameValue}' has an invalid format");
                }

                var orderId = orderIdMatch.Groups[1].Value;
                var date = GetDateFromOrderedMenuDayAttribute(dateInputNode.Attributes["value"].Value);
                var mealName = titleNode.GetInnerText();
                var isOrderCancelable = orderItemNode.GetNodes(".//div[@class='cancel']").Any();
                bool isOrderApproved;

                var orderApprovedInputNode = orderItemNode.SelectSingleNode($".//input[@name='ITM_SelectedRotationId_{orderId}' and @type='radio']");
                if (orderApprovedInputNode != null)
                {
                    isOrderApproved = orderApprovedInputNode.Attributes["class"].Value == "greentext";
                }
                else
                {
                    var checkMarkIconNode = orderItemNode.SelectSingleNode(".//span[@class='checkmark']//i[@class='fa fa-check']");
                    isOrderApproved = checkMarkIconNode != null;
                }

                var orderedMeal = new OrderedGourmetMenuMeal(orderId, mealName, isOrderApproved, isOrderCancelable);

                days.Add(new OrderedGourmetMenuDay(date, orderedMeal));
            }

            return new OrderedGourmetMenu(days);
        }

        private static IReadOnlyDictionary<string, string> ParseConfirmParametersFromOrderedGourmetMenuHtml(string html)
        {
            var parameters = new Dictionary<string, string>();

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var formNode = document.DocumentNode.GetSingleNode("//form[@action='default.aspx' and @method='post' and @name='genericform']");

            foreach (var inputNode in formNode.GetNodes(".//*[self::input or self::select]"))
            {
                var name = inputNode.Attributes["name"]?.Value;

                if (!string.IsNullOrEmpty(name))
                {
                    parameters.Add(name, inputNode.Attributes["value"]?.Value ?? string.Empty);
                }
            }

            var confirmButtonNode = formNode.GetSingleNode(".//button[@name='btn_order_confirm']");
            parameters.Add(confirmButtonNode.Attributes["name"].Value, confirmButtonNode.Attributes["value"].Value);

            return parameters;
        }

        private static IReadOnlyList<BillingPosition> ParseBillingPositionsFromBillingHtml(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var billingPositions = new List<BillingPosition>();
            var tableBodyNode = document.DocumentNode.GetSingleNode("//div[@class='abrechnung']//table[contains(@class, 'table-bordered')]//tbody");

            foreach (var rowNode in tableBodyNode.GetNodes(".//tr"))
            {
                var dateNode = rowNode.SelectSingleNode(".//td[@data-title='Datum']");
                if (dateNode == null)
                {
                    continue;
                }

                var countNode = rowNode.GetSingleNode(".//td[@data-title='Stk.']");
                var mealNameNode = rowNode.GetSingleNode(".//td[@data-title='Speise']");
                var costNode = rowNode.GetSingleNode(".//td[@data-title='Abrechnung']");

                var date = GetDateFromBillingEntryDateString(dateNode.GetInnerText());
                var mealName = mealNameNode.GetInnerText();
                var countString = countNode.GetInnerText();
                var costString = costNode.GetInnerText().Replace("€", string.Empty).Trim();

                if (!int.TryParse(countString, out var count))
                {
                    throw new InvalidOperationException($"Count '{countString}' has an invalid format");
                }

                if (!double.TryParse(costString, new CultureInfo("de-DE"), out var cost))
                {
                    throw new InvalidOperationException($"Cost '{costString}' has an invalid format");
                }

                billingPositions.Add(new BillingPosition(date, false, BillingPositionType.Meal, mealName, count, cost));
            }

            return billingPositions;
        }

        private static DateTime GetDateFromMenuDayNodeValue(string nodeValue)
        {
            var splitValue = nodeValue.Split('.');
            if (splitValue.Length != 2)
            {
                throw new InvalidOperationException($"Expected two values after splitting the date node value '{nodeValue}' but there are {splitValue.Length} values");
            }

            var dayString = splitValue[0];
            var monthString = splitValue[1];

            if (!int.TryParse(dayString, out var day))
            {
                throw new InvalidOperationException($"Could not parse value '{dayString}' for day as integer");
            }

            if (!int.TryParse(monthString, out var month))
            {
                throw new InvalidOperationException($"Could not parse value '{monthString}' for month as integer");
            }

            return new DateTime(DateTime.Now.Year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static DateTime GetDateFromOrderedMenuDayAttribute(string attributeValue)
        {
            var spaceSplitValue = attributeValue.Split(' ');
            if (spaceSplitValue.Length != 2)
            {
                throw new InvalidOperationException($"Expected three values after splitting the date node value '{attributeValue}' but there are {spaceSplitValue.Length} values");
            }

            var dateSplitValue = spaceSplitValue[0].Split('.');
            if (dateSplitValue.Length != 3)
            {
                throw new InvalidOperationException($"Expected three values after splitting the date node value '{attributeValue}' but there are {spaceSplitValue.Length} values");
            }

            var dayString = dateSplitValue[0];
            var monthString = dateSplitValue[1];
            var yearString = dateSplitValue[2];

            if (!int.TryParse(dayString, out var day))
            {
                throw new InvalidOperationException($"Could not parse value '{dayString}' for day as integer");
            }

            if (!int.TryParse(monthString, out var month))
            {
                throw new InvalidOperationException($"Could not parse value '{monthString}' for month as integer");
            }

            if (!int.TryParse(yearString, out var year))
            {
                throw new InvalidOperationException($"Could not parse value '{year}' for year as integer");
            }

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static DateTime GetDateFromBillingEntryDateString(string dateString)
        {
            var dateSplitValue = dateString.Split('.');
            if (dateSplitValue.Length != 3)
            {
                throw new InvalidOperationException($"Expected three values after splitting the date node value '{dateString}' but there are {dateSplitValue.Length} values");
            }

            var dayString = dateSplitValue[0];
            var monthString = dateSplitValue[1];
            var yearString = dateSplitValue[2];

            if (!int.TryParse(dayString, out var day))
            {
                throw new InvalidOperationException($"Could not parse value '{dayString}' for day as integer");
            }

            if (!int.TryParse(monthString, out var month))
            {
                throw new InvalidOperationException($"Could not parse value '{monthString}' for month as integer");
            }

            if (!int.TryParse(yearString, out var year))
            {
                throw new InvalidOperationException($"Could not parse value '{yearString}' for year as integer");
            }

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static IReadOnlyDictionary<string, string> GetUrlParametersForPage(string pageName, string actionName = null, IReadOnlyDictionary<string, string> parameters = null)
        {
            var pageParameters = new Dictionary<string, string>
            {
                { "c", pageName }
            };

            if (!string.IsNullOrEmpty(actionName))
            {
                pageParameters.Add("a", actionName);
            }

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    pageParameters.Add(parameter.Key, parameter.Value);
                }
            }

            return pageParameters;
        }
    }
}
