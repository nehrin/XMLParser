using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XMLParser
{
    public class ExtractionHelper
    {
        private string Text;
        private HttpClient client;
        private bool unmatchedTag;

        public ExtractionHelper(string text)
        {
            Text = text;
            unmatchedTag = false;
            client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:56458/api");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        }

        public async Task extractAsync()
        {
            bool totalFound = false;
            Regex rg = new Regex("<[^<>]+>");
            Match match = rg.Match(Text);
            MatchCollection matches = rg.Matches(Text);

            string node_str = match.ToString();
            string match_expense = node_str.Substring(1, node_str.Length - 2);
            double total = 0.0;
            string cost_centre = "";
            string payment_method = "UNKNOWN";
            string vendor = "";
            string description = "";
            string date_str = "";
            DateTime date = new DateTime();
            for (int i = 1; i < matches.Count; i++)
            {
                string xml = matches[i].ToString();
                if (xml.Contains("total"))
                {
                    totalFound = true;
                    total = Convert.ToDouble(extractValue("total>"));
                }
                if (xml.Contains("cost_centre"))
                    cost_centre = extractValue("cost_centre>");
                if (xml.Contains("<payment_method>"))
                    payment_method = extractValue("payment_method>");
                if (xml.Contains("vendor>"))
                    vendor = extractValue("vendor>");
                if (xml.Contains("<description>"))
                    description = extractValue("description>");
                if (xml.Contains("date>"))
                {
                    date_str = extractValue("date>");
                    Regex reg_space = new Regex(" ");
                    MatchCollection match_space = reg_space.Matches(date_str);

                    string day = date_str.Substring(0, match_space[0].Index);
                    string dateNumber = date_str.Substring(match_space[0].Index, match_space[1].Index-match_space[0].Index).Trim();
                    string month = date_str.Substring(match_space[1].Index, match_space[2].Index - match_space[1].Index).Trim();
                    string year = date_str.Substring(date_str.Length - 4).Trim();
                    date = Convert.ToDateTime(dateNumber+"-" + month + "-"+year);
                }
            }
            if (totalFound && !unmatchedTag)
            {
                if (match_expense.Trim().Equals("expense"))
                {
                    Expense expense = new Expense
                    {
                        costCentre = cost_centre,
                        total = total,
                        paymentMethod = payment_method
                    };

                    var uri = await Post<Expense>("http://localhost:56458/api/expense", expense);
                    Console.WriteLine($"Created at {uri}");

                    var expenseBack = await GetExpenseAsync(uri.AbsoluteUri);
                    ShowExpense(expenseBack);
                }

                if (Text.Contains("reservation"))
                {
                    Reservation reservation = new Reservation
                    {
                        vendor = vendor,
                        description = description,
                        reservationTime = date
                    };

                    var uri_reservation = await Post<Reservation>("http://localhost:56458/api/reservation", reservation);
                    var reservationBack = await GetReservationAsync(uri_reservation.AbsoluteUri);
                    ShowReservation(reservationBack);
                }
            }
            else
            {
                Console.WriteLine("Invalid input in the message.");
            }
            
            Console.ReadLine();
        }

        private string extractValue(string match_str)
        {
            Regex rg_end = new Regex(match_str);
            MatchCollection matches_end = rg_end.Matches(Text);
            string xml;
            if (matches_end.Count == 2)
            {
                int match_end = matches_end[1].Index;
                int match_start = matches_end[0].Index;
                xml = Text.Substring(match_start + match_str.Length, match_end - match_start - match_str.Length - 2);

                Regex rg_return = new Regex("\r\n");
                xml = rg_return.Replace(xml, string.Empty);
            }
            else
            {
                xml = "NOT FOUND";
                unmatchedTag = true;
            }
            return xml;
        }

        public async Task<Uri> Post<T>(string url, Expense expense)
        {
            using (var client = new HttpClient())
            {
                string JSONexpense = JsonConvert.SerializeObject(expense);
                var content = new StringContent(JSONexpense, Encoding.UTF8, "application/json");
                var result = await client.PostAsync(url, content);
                Console.WriteLine(result);
                return result.Headers.Location;
            }
        }

        public async Task<Uri> Post<T>(string url, Reservation reservation)
        {
            string JSONexpense = JsonConvert.SerializeObject(reservation);
            var content = new StringContent(JSONexpense, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(url, content);
            Console.WriteLine(result);
            return result.Headers.Location;
        }

        public void ShowExpense(Expense expense)
        {
            Console.WriteLine($"ID: {expense.id}\t" + $"Cost Centre: {expense.costCentre}\tTotal: " +
                $"{expense.total}\tPayment mehtod: {expense.paymentMethod}");
        }

        public void ShowReservation(Reservation reservation)
        {
            Console.WriteLine($"ID: {reservation.id}\t" + $"Decription: {reservation.description}\tVendor: {reservation.vendor}");
        }

        public async Task<Expense> GetExpenseAsync(string path)
        {
            Expense expense = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                expense = await response.Content.ReadAsAsync<Expense>();
            }
            return expense;
        }

        public async Task<Reservation> GetReservationAsync(string path)
        {
            Reservation reservation = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                reservation = await response.Content.ReadAsAsync<Reservation>();
            }
            return reservation;
        }

        public async Task<Expense> UpdateExpenseAsync(Expense expense)
        {
            HttpResponseMessage response = await client.PutAsJsonAsync($"api/expense/{expense.id}", expense);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            expense = await response.Content.ReadAsAsync<Expense>();
            return expense;
        }

        public async Task<HttpStatusCode> DeleteExpenseAsync(string id)
        {
            HttpResponseMessage response = await client.DeleteAsync(
                $"api/expense/{id}");
            return response.StatusCode;
        }
    }
}
