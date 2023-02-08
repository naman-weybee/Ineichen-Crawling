using HtmlAgilityPack;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CsharpWebScraping
{
    class Program
    {
        static void Main(string[] args)
        {
            GetDocument();
            Console.ReadLine();
        }

        private static void GetDocument()
        {
            SqlConnection con = null!;
            try
            {
                HtmlWeb web = new();
                HtmlDocument doc = web.Load("https://ineichen.com/auctions/past/");

                con = new SqlConnection("data source=DESKTOP-9J2CV47; database=Ineichen; integrated security=SSPI");
                con.Open();

                var Details = doc.DocumentNode.SelectNodes("//div[@class='auction-date-location']");
                var Times = doc.DocumentNode.SelectNodes("//div[@class='auction-date-location']/div[1]");
                var Locations = doc.DocumentNode.SelectNodes("//div[@class='auction-date-location']/div[2]");
                var Titles = doc.DocumentNode.SelectNodes("//h2[@class='auction-item__name']");
                var LotCounts = doc.DocumentNode.SelectNodes("//div[@class='auction-item__btns']/a");
                var ImageSources = doc.DocumentNode.SelectNodes("//a[(@class='auction-item__image')]/img");
                var Links = doc.DocumentNode.SelectNodes("//a[@class='auction-item__image']");
                Regex regex = new(@"^\s?(\d+)\s?(\w+)?\s?(\d{4})?\,?\s?(\d{2}[\,\:]\d{2})?\,?\s?(\(CET\))?\,?\s?((-)?\s?(\d+)?\s?(\w+)?\s?(\d{4})?\,?\s?(\d{2}[\,\:]\d{2})?\,?\s?(\(CET\))?\,?\s?)?", RegexOptions.IgnoreCase);

                for (int i = 0; i < Details.Count; i++)
                {
                    string Title = null!;
                    string LotCount = "";
                    var StartDate = "";
                    var StartMonth = "";
                    var StartYear = "";
                    var StartTime = "";
                    var EndDate = "";
                    var EndMonth = "";
                    var EndYear = "";
                    var EndTime = "";

                    var dateMatchRegex = regex.Match(Times[i].InnerText.Trim().Replace("\n", ""));
                    var src = ImageSources[i].GetAttributes("src");
                    string ImageURL = "https://ineichen.com" + src.First().Value;
                    Console.WriteLine("ImageURL : " + ImageURL);

                    var link = Links?[i]?.GetAttributes("href");
                    string Link = "https://ineichen.com" + link!.First().Value;
                    Console.WriteLine("Link : " + Link);

                    bool titleFlag = Regex.IsMatch(LotCounts[i].InnerHtml.Trim(), @"[0-9]");

                    if (titleFlag)
                    {
                        Title = Titles[i]!.SelectNodes("./a")!.First()!.InnerHtml!;
                        Console.WriteLine("Title : " + Title);

                        LotCount = Regex.Replace(LotCounts[i].InnerHtml.Trim(), @"[^\d]", "");
                        Console.WriteLine("LotCount : " + LotCount);
                    }
                    else
                    {
                        Title = Titles[i].InnerHtml.Trim();
                        Console.WriteLine("Title : " + Title);

                        LotCount = "Pdf Catalog OR Explore Link";
                        Console.WriteLine("LotCount : " + LotCount);
                    }

                    if (dateMatchRegex.Success)
                    {
                        StartDate = dateMatchRegex.Groups[1].Value;
                        StartMonth = dateMatchRegex.Groups[2].Value;
                        StartYear = dateMatchRegex.Groups[3].Value;
                        StartTime = dateMatchRegex.Groups[4].Value;
                        EndDate = dateMatchRegex.Groups[8].Value;
                        EndMonth = dateMatchRegex.Groups[9].Value;
                        EndYear = dateMatchRegex.Groups[10].Value;
                        EndTime = dateMatchRegex.Groups[11].Value;
                    }

                    string location = Locations[i].InnerText.Trim();
                    Console.WriteLine("Location: " + location.Trim());

                    string Time = Times[i].InnerText.Trim();
                    string Location = Locations[i].InnerText.Trim();
                    string Description = Time + ", " + Location;
                    Console.WriteLine("Description : " + Description.Trim());
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();

                    SqlCommand cm = new("SELECT id FROM Auctions WHERE Title in ('" + Title.Trim() + "')", con);
                    int id = Convert.ToInt32(cm.ExecuteScalar());
                    SqlDataReader sdr = cm.ExecuteReader();
                    sdr.Read();
                    bool IsAuctionExist = false;
                    if (sdr.HasRows)
                    {
                        if (sdr.GetInt32(0) == id)
                        {
                            IsAuctionExist = true;
                        }
                        sdr.Close();
                    }

                    if (IsAuctionExist)
                    {
                        sdr.Close();
                        SqlCommand cmd = new("SP_UpdateData", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Title", Title);
                        cmd.Parameters.AddWithValue("@Description", Description);
                        cmd.Parameters.AddWithValue("@ImageUrl", ImageURL);
                        cmd.Parameters.AddWithValue("@Link", Link);
                        cmd.Parameters.AddWithValue("@LotCount", LotCount);
                        cmd.Parameters.AddWithValue("@StartDate", StartDate);
                        cmd.Parameters.AddWithValue("@StartMonth", StartMonth);
                        cmd.Parameters.AddWithValue("@StartYear", StartYear);
                        cmd.Parameters.AddWithValue("@StartTime", StartTime);
                        cmd.Parameters.AddWithValue("@EndDate", EndDate);
                        cmd.Parameters.AddWithValue("@EndMonth", EndMonth);
                        cmd.Parameters.AddWithValue("@EndYear", EndYear);
                        cmd.Parameters.AddWithValue("@EndTime", EndTime);
                        cmd.Parameters.AddWithValue("@Location", location);
                        cmd.ExecuteNonQuery();
                    }
                    else if (!IsAuctionExist)
                    {
                        sdr.Close();
                        SqlCommand cmd = new("SP_InsertData", con)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Title", Title);
                        cmd.Parameters.AddWithValue("@Description", Description);
                        cmd.Parameters.AddWithValue("@ImageUrl", ImageURL);
                        cmd.Parameters.AddWithValue("@Link", Link);
                        cmd.Parameters.AddWithValue("@LotCount", LotCount);
                        cmd.Parameters.AddWithValue("@StartDate", StartDate);
                        cmd.Parameters.AddWithValue("@StartMonth", StartMonth);
                        cmd.Parameters.AddWithValue("@StartYear", StartYear);
                        cmd.Parameters.AddWithValue("@StartTime", StartTime);
                        cmd.Parameters.AddWithValue("@EndDate", EndDate);
                        cmd.Parameters.AddWithValue("@EndMonth", EndMonth);
                        cmd.Parameters.AddWithValue("@EndYear", EndYear);
                        cmd.Parameters.AddWithValue("@EndTime", EndTime);
                        cmd.Parameters.AddWithValue("@Location", location);
                        cmd.ExecuteNonQuery();
                    }
                }
                Console.WriteLine("Data Inserted/Updated Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadKey();
        }
    }
}