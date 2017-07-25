using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteRouteStats
{
    class Program
    {
        static void Main(string[] args)
        {

            Directory.CreateDirectory("./samples");

            bool first = true;
            string timestampFirst = string.Empty;
            string timestampLast = string.Empty;
            int jumpCount = 0;
            float jumpTotalDist = 0f;
            float totalFuel = 0f;
            Dictionary<string, long> BodyTypes = new Dictionary<string, long>();
            long bodiesFound = 0;


            // Get all the files we want to parse
            DirectoryInfo di = new DirectoryInfo("./samples");
            FileInfo[] fileInfo = di.GetFiles().OrderBy(f => f.FullName).ToArray();
            if (fileInfo.Length == 0)
            {
                Console.WriteLine("No files to parse for data. Place the journal files you want to use for data in the \"samples\" folder and run again.");
#if DEBUG
                Console.ReadLine();
#endif
            }
            else
            {
                Console.WriteLine("Parsing samples. Depending on number and size of samples, this may take a while.");

                foreach (FileInfo f in fileInfo)
                {
#if DEBUG
                    Console.WriteLine("--> "+f.FullName);
#endif
                    using (StreamReader sr = new StreamReader(f.FullName))
                    {
                        string line = string.Empty;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)) continue;
                            JObject json = JObject.Parse(line);
                            string e = json.GetValue("event").ToString();
                            if (!e.Equals("FSDJump") && !e.Equals("Scan")) continue;
                            // { "timestamp":"2017-07-18T16:16:58Z", "event":"FSDJump", "StarSystem":"Byua Euq EG-A b28-7", "StarPos":[-1343.500,110.938,5944.938], "SystemAllegiance":"", "SystemEconomy":"$economy_None;", "SystemEconomy_Localised":"None", "SystemGovernment":"$government_None;", "SystemGovernment_Localised":"None", "SystemSecurity":"$SYSTEM_SECURITY_low;", "SystemSecurity_Localised":"Low Security", "JumpDist":59.706, "FuelUsed":7.495902, "FuelLevel":24.504097 }
                            if (e.Equals("FSDJump"))
                            {
                                string timestamp = json.GetValue("timestamp").ToString();
                                float jumpDist = json.GetValue("JumpDist").ToObject<float>();
                                float fuel = json.GetValue("FuelUsed").ToObject<float>();

                                totalFuel += fuel;
                                jumpTotalDist += jumpDist;
                                jumpCount++;

                                if (first)
                                {
                                    first = false;
                                    timestampFirst = timestamp;
                                }
                                timestampLast = timestamp;
                            }
                            else if (e.Equals("Scan")) {
                                string bodyType = string.Empty;
                                try
                                {
                                    bool terraformable = json.GetValue("TerraformState").ToString().Equals("Terraformable");
                                    bodyType = (terraformable ? "Terraformable " : "") + json.GetValue("PlanetClass").ToString().Replace("Sudarsky class", "Class");

                                }
                                catch { bodyType = string.Format("{0}-class star", json.GetValue("StarType").ToString()); }
                                bodiesFound++;
                                if (BodyTypes.ContainsKey(bodyType))
                                    BodyTypes[bodyType]++;
                                else
                                    BodyTypes.Add(bodyType, 1);
                            }
                        }
                    }
                }
#if !DEBUG
                Console.Clear();
#endif

                Console.WriteLine(string.Format("Total jump count: {0}", jumpCount.ToString("0,0")));
                Console.WriteLine(string.Format("Total jump distance (ly): {0}", jumpTotalDist.ToString("0,0.00")));
                Console.WriteLine(string.Format("Average jump distance (ly): {0}", (jumpTotalDist / (float)jumpCount).ToString("0,0.00")));
                Console.WriteLine(string.Format("Number of bodies scanned: {0}", bodiesFound));
                var ordered = BodyTypes.OrderByDescending(a => a.Value);
                foreach (KeyValuePair<string, long> kvp in ordered)
                    Console.WriteLine(string.Format("\t{0}: {1:n0}", kvp.Key, kvp.Value));
                Console.WriteLine();
                Console.WriteLine(string.Format("Total fuel usage (t): {0}", totalFuel.ToString("0,0.00")));
                Console.WriteLine(string.Format("Start time (UTC): {0}", timestampFirst));
                Console.WriteLine(string.Format("End time (UTC): {0}", timestampLast));

                DateTime d1 = DateTime.Parse(timestampFirst, null, System.Globalization.DateTimeStyles.RoundtripKind);
                DateTime d2 = DateTime.Parse(timestampLast, null, System.Globalization.DateTimeStyles.RoundtripKind);
                TimeSpan diff = (d2 - d1);

                Console.WriteLine(string.Format("Expedition time: {0}", diff.ToString(@"d\:hh\:mm\:ss")));

                Console.ReadLine();
            }
        }
    }
}