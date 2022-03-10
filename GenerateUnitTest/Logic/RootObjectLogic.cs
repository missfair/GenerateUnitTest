using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerateUnitTest.Logic
{
    public static class RootObjectLogic
    {
        public static object MapRootAssertExpect(this object request)
        {
            string resultScripts = string.Empty;
            List<string> listExpectScripts = new List<string>();
            string json = JsonConvert.SerializeObject(request);
            if (json == null)
            {
                return new
                {
                    ErrorMessage = "ไม่สามารถแปลง json จาก body เป็น Objectได้ กรุณาเช็ค json format อีกครั้ง"
                };
            }

            bool isArrayObj = false;
            JObject jObj = new JObject();
            List<JObject> jObjs = new List<JObject>();
            try
            {
                jObj = JObject.Parse(json);
                jObjs.Add(jObj);
            }
            catch (Exception)
            {
                isArrayObj = true;
                JArray array = JArray.Parse(json);
                List<JToken> jTokens = array.Children().ToList();
                foreach (var item in jTokens)
                {
                    var dasd = item.ToObject<JObject>();
                    jObjs.Add(dasd);
                }
            }

            int inputNumber = 0;
            string pattern = "Assert.AreEqual(result[Index].ChildrenPath, \"{expectValue}\");";

            foreach (var jos in jObjs)
            {
                foreach (var item in jos)
                {
                    string script = string.Empty;
                    JToken jResponse = jos[item.Key];
                    if (isArrayObj)
                    {
                        script = pattern.Replace("Index", inputNumber.ToString());
                    }
                    else
                    {
                        script = pattern.Replace("[Index]", string.Empty);
                    }
                    List<string> resultMaps = MapAssertExpect(jResponse, script).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    listExpectScripts.AddRange(resultMaps);
                }
                inputNumber++;
            }
            resultScripts = string.Join(Environment.NewLine, listExpectScripts).Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            return  resultScripts;
        }

        private static List<string> MapAssertExpect(JToken jToken, string script)
        {
            List<string> scripts = new List<string>();
            var childObjs = jToken.Children().ToList();
            if (childObjs.Count() > 1)
            {
                foreach (var children in childObjs)
                {
                    scripts.AddRange(MapAssertExpect(children, script));
                }
            }
            else if (childObjs.Count == 1)
            {
                var childObj = childObjs.FirstOrDefault();
                var path = childObj.Path;
                string value = "";
                bool byPass = false;
                try
                {
                    value = convertFormat(childObj.Value<string>()).ToLower();
                }
                catch (Exception)
                {
                    var childObjxx = childObj.Children().ToList();
                    foreach (var item in childObjxx)
                    {
                        scripts.AddRange(MapAssertExpect(item, script));
                        byPass = true;
                    }
                }
                if (!byPass)
                {
                    string expectScript = mapExpectScript(script, path, value);
                    scripts.Add(expectScript + Environment.NewLine);
                }

            }
            else if (childObjs.Count() == 0)
            {
                var path = jToken.Path;
                try
                {
                    var value = convertFormat(jToken.Value<string>()).ToLower();
                    string expectScript = mapExpectScript(script, path, value);
                    scripts.Add(expectScript + Environment.NewLine);
                }
                catch (Exception)
                {
                }
            }
            return scripts;
        }

        public static string mapExpectScript(string script, string path, string value)
        {
            string expectScript = script.Replace("ChildrenPath", path);
            bool isNewLineOnText = value.Contains(Environment.NewLine) || value.Contains("\n");
            if (!isNewLineOnText)
            {
                expectScript = expectScript.Replace("{expectValue}", value);
            }
            return expectScript;
        }
        public static string convertFormat(string value)
        {
            DateTime dDate;
            decimal number;
            if (DateTime.TryParse(value, out dDate) && decimal.TryParse(value.ToString(), out number) == false)
            {
                String.Format("{0:d/MM/yyyy}", dDate);
                value = String.Format("{0:s}", dDate);
            }
            return value;
        }

    }
}
