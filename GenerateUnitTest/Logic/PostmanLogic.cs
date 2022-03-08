using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenerateUnitTest.Logic
{
    public static class PostmanLogic
    {
        public static object WrtieMockRegression(this object request)
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

            int index = 0;
            //string pattern = "pm.expect(result[Index].ChildrenPath).to.eql(Value);";
            string pattern = "Assert.AreEqual(result[Index].ChildrenPath, \"Value\");";

            foreach (var jos in jObjs)
            {
                foreach (var item in jos)
                {
                    string script = string.Empty;
                    JToken jResponse = jos[item.Key];
                    if (isArrayObj)
                    {
                        script = pattern.Replace("Index", index.ToString());
                    }
                    else
                    {
                        script = pattern.Replace("[Index]", string.Empty);
                    }
                    List<string> resultMaps = MapMockRegression(jResponse, script).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    listExpectScripts.AddRange(resultMaps);
                }
                index++;
            }
            resultScripts = string.Join(Environment.NewLine, listExpectScripts).Replace(Environment.NewLine+ Environment.NewLine, Environment.NewLine);
            return /*"var jsonData = pm.response.json();" + Environment.NewLine +*/ resultScripts;
        }

        private static List<string> MapMockRegression(JToken jToken, string script)
        {
            List<string> scripts = new List<string>();
            var childObjs = jToken.Children().ToList();
            if (childObjs.Count() > 1)
            {
                foreach (var children in childObjs)
                {
                    scripts.AddRange(MapMockRegression(children, script));
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
                        scripts.AddRange(MapMockRegression(item, script));
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
            //string expectPattern = @"pm.test( " + "\"Path is Value\"" + ", function () { Expect});";
            string expectScript = script.Replace("ChildrenPath", path /*+ ".toString()").Replace("Value", "'" + value + "'"*/);
            //expectScript = expectPattern.Replace("Path", path).Replace("Value", value).Replace("Expect", Environment.NewLine + "   " + expectScript + Environment.NewLine);
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
