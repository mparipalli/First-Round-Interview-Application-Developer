using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ManaswiniParipalli
{
    public class Completion
    {
        public string name { get; set; }
        public string timestamp { get; set; }
        public object expires { get; set; }
    }

    public class Root
    {
        public string name { get; set; }
        public List<Completion> completions { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string textlink = @"trainings (correct).txt";
            List<string> ListOfTrainingsNames = new List<string>()
            {
                "Electrical Safety for Labs",
                "X-Ray Safety",
                "Laboratory Safety Training"
            };
            bool isModified = false;
            int FiscalYear = 2024;
            DateTime FiscalYearStartDate = new DateTime(FiscalYear - 1, 7, 1);
            DateTime FiscalYearEndDate = new DateTime(FiscalYear, 6, 30);
            DateTime ExpirationDate = new DateTime(2023, 10, 1);

            try
            {
                if (!File.Exists(textlink))
                {
                    Console.WriteLine($"File not found: {textlink}");
                    return;
                }

                string JsonContent = File.ReadAllText(textlink);
                List<Root> roots = JsonConvert.DeserializeObject<List<Root>>(JsonContent);

                // Task 1
                //Query
                var DistinctTrainingNamesWithCount = roots
                    .SelectMany(r => r.completions.Select(c => new { r.name, Completion = c }))
                    .GroupBy(x => new { x.Completion.name, PersonName = x.name })
                    .Select(g => g.OrderByDescending(x => DateTime.Parse(x.Completion.timestamp)).First())
                    .GroupBy(x => x.Completion.name)
                    .Select(g => new { TrainingName = g.Key, Count = g.Count() })
                    .ToList();
                //Creating the file and saving the data
                string path1 = "Task1.json";
                File.WriteAllText(path1, JsonConvert.SerializeObject(DistinctTrainingNamesWithCount, Formatting.Indented));
                Console.WriteLine($"Distinct training names and counts have been saved to {Path.GetFullPath(path1)}");

                // Task 2
                //Incase if the user wants to enter input
                Console.WriteLine("Enter the fiscal year in this format 2024 for Task 2 or press Enter to use default which is 2024 ");
                string Task2FiscalYearInput = Console.ReadLine();

                if (!string.IsNullOrEmpty(Task2FiscalYearInput))
                {
                    if (int.TryParse(Task2FiscalYearInput, out int inputFiscalYear))
                    {
                        
                        FiscalYearStartDate = new DateTime(inputFiscalYear - 1, 7, 1);
                        FiscalYearEndDate = new DateTime(inputFiscalYear, 6, 30);
                        isModified = true; 
                    }
                }


                Console.WriteLine("Enter training names separated by commas in this format 'X-Ray Safety, Laboratory Safety Training' or press Enter to use default which is (\"Electrical Safety for Labs\", \"X-Ray Safety\", \"Laboratory Safety Training\")");
                string userInputTrainings = Console.ReadLine();

                if (!string.IsNullOrEmpty(userInputTrainings))
                {
                    
                    ListOfTrainingsNames = userInputTrainings.Split(',')
                                                             .Select(t => t.Trim()) 
                                                             .ToList();
                    isModified = true; 
                }
                //Query
                var ListAllPeopleNames = roots
                                         .SelectMany(r => r.completions.Select(c => new
                                         {
                                         PersonName = r.name,
                                         TrainingName = c.name,
                                         Timestamp = DateTime.Parse(c.timestamp).ToString("M/d/yyyy")
                                         }))
                                        .Where(x =>
                                        ListOfTrainingsNames.Contains(x.TrainingName) &&
                                        DateTime.TryParse(x.Timestamp, out DateTime completionDate) &&
                                        completionDate >= FiscalYearStartDate && completionDate <= FiscalYearEndDate)
                                        .GroupBy(x => new { x.PersonName, x.TrainingName }) 
                                        .Select(g => g.OrderByDescending(x => DateTime.Parse(x.Timestamp)).First())  
                                        .GroupBy(x => x.TrainingName)
                                        .SelectMany(g => new object[] { new { TrainingName = g.Key } }
                                        .Concat(g.Select(person => new
                                        {
                                        person.PersonName,
                                        person.Timestamp
                                        })))
                                        .ToList();


                //Creating the file and saving the data
                string path2 = isModified ? "Task2overwritten.json" : "Task2.json";
                File.WriteAllText(path2, JsonConvert.SerializeObject(ListAllPeopleNames, Formatting.Indented));
                Console.WriteLine($"List All People Names have been saved to {Path.GetFullPath(path2)}");

                // Task 3
                //Incase if the user wants to enter input 
                Console.WriteLine("Enter the expiration date for Task 3 in this format Oct 1st, 2023 or press Enter to use default which is Oct 1st, 2023");
                string Task3ExpirationDateInput = Console.ReadLine();
                //Processing the input provided by the user and converting it into datetime format
                if (!string.IsNullOrEmpty(Task3ExpirationDateInput))
                {
                    string processedDateInput = Regex.Replace(Task3ExpirationDateInput, @"\b(\d+)(st|nd|rd|th)\b", "$1");

                    if (DateTime.TryParse(processedDateInput, out DateTime inputExpirationDate))
                    {
                        ExpirationDate = inputExpirationDate;
                    }
                    else
                    {
                        Console.WriteLine("Could not parse the provided date. Using default expiration date.");
                    }
                }

                Console.WriteLine($"Expiration Date to compare against: {ExpirationDate.ToString("M/d/yyyy")}");


                //Query 
                var ListOfAllCompletedTrainings = roots
                                                .SelectMany(r => r.completions.Select(c => new { PersonName = r.name, Completion = c }))
                                                .Where(c =>
                                                ListOfTrainingsNames.Any(training => c.Completion.name.Contains(training)) &&
                                                DateTime.TryParse(c.Completion.timestamp, out DateTime completionDate))
                                                .GroupBy(c => new { c.PersonName, c.Completion.name })  
                                                .Select(g => g.OrderByDescending(c => DateTime.Parse(c.Completion.timestamp)).First())  
                                                .Select(c =>
                                                {
                                                bool isExpirationDateParsed = DateTime.TryParse(c.Completion.expires?.ToString(), out DateTime expirationDate);
                                                if (!isExpirationDateParsed)
                                                {
                                                return null; 
                                                }
                                                return new
                                                {
                                                PersonName = c.PersonName,
                                                TrainingName = c.Completion.name,
                                                Status = expirationDate < ExpirationDate
                                                ? "Expired"
                                                : (expirationDate <= ExpirationDate.AddMonths(1) ? "Expires Soon" : null)
                                                };
                                                })
                                                .Where(c => c != null && c.Status != null)  
                                                .ToList();

                //Creating the file and saving the data
                string path3 = string.IsNullOrEmpty(Task3ExpirationDateInput) ? "Task3.json" : "Task3overwritten.json";
                System.IO.File.WriteAllText(path3, JsonConvert.SerializeObject(ListOfAllCompletedTrainings, Formatting.Indented));
                Console.WriteLine($"List of all Completed Trainings have been saved to {System.IO.Path.GetFullPath(path3)}");

                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }

            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: File not found at path: {textlink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
