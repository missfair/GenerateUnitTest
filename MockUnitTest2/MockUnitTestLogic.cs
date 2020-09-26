using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MockUnitTest2
{
    public static class MockUnitTestLogic
    {
        public static List<string> methodNames { get; private set; } = new List<string>();
        //exaple
        //var resultMockUnitTest = MethodBase.GetCurrentMethod().GetMockUnitTests( addOnPaymentConfigs, gracePeriodRequest);
        public static string GetMockUnitTests(this MethodBase methodBase, params object[] requestObjects)
        {
            List<string> codeMock = new List<string>();
            List<string> paramRequest = new List<string>();

            foreach (var model in requestObjects)
            {
                if (model == null)
                {
                    paramRequest.Add(null);
                }
                else
                {
                    var mockInputUnitTest = GetMockInputUnitTest(methodBase, model, model.GetType().Name);
                    codeMock.Add(mockInputUnitTest.codeMockInput);
                    paramRequest.Add(mockInputUnitTest.methodName);
                }

            }
            string codeMockInputs = string.Join(Environment.NewLine, codeMock);
            string codeMockCallFunction = GetMockCallMethod(methodBase, paramRequest);
            string resultCodeMock = codeMockInputs + Environment.NewLine + GetCodeMethodTest(methodBase, codeMockCallFunction);
            return resultCodeMock;
        }

        private static string GetCodeMethodTest(MethodBase methodBase, string textCalMethod)
        {
            string pattern =
                            "[TestMethod]" + "\r\n" +
                            $"public void {methodBase.Name}_Test()" + "\r\n" +
                            "{" + "\r\n" +
                            $"{textCalMethod}" + "\r\n" +
                            "}";
            return pattern;
        }
        private static string GetMockCallMethod(MethodBase methodBase, List<string> objectNames)
        {
            string className = methodBase.DeclaringType.Name;
            string newInstance = (methodBase.IsStatic) ? string.Empty : "new";
            string passRequests = string.Join(" ,", objectNames);
            string pattern = $"var result = {newInstance} {className}.{methodBase.Name}({passRequests});";
            return pattern;
        }

        private static (string codeMockInput, string methodName) GetMockInputUnitTest(MethodBase methodBase, object objects, string returnType)
        {
            string methodName = $"GetMock_{returnType}_{methodBase.Name}";
            methodNames.Add(methodName);
            if (methodNames.Contains(methodName))
            {
                methodName = methodName + (methodNames.Count(a => methodNames.Contains(methodName)) + 1);
            }
            string first = $"public {returnType} {methodName}(){Environment.NewLine}" + "{";
            string rawJson, rawJsonAfterConvertToTextAssembly;
            try
            {
                rawJson = JsonConvert.SerializeObject(objects);
                rawJsonAfterConvertToTextAssembly = convertJsonToText(rawJson);
            }
            catch (Exception)
            {
                rawJsonAfterConvertToTextAssembly = "null";
            }

            string declarRawJson = $"string raw = \"{rawJsonAfterConvertToTextAssembly}\";";
            string deserializeObjectPattern = $"return JsonConvert.DeserializeObject<{returnType}>(raw);";
            string codeMockInput = first + declarRawJson + Environment.NewLine + deserializeObjectPattern + Environment.NewLine + "}";
            return (codeMockInput, $"{methodName}()");
        }


        private static string convertJsonToText(string data)
        {
            var dataList = data.Split('"').ToList();
            return String.Join(@"\" + "\"", dataList.ToArray());
        }

    }
}
