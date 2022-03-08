using GenerateUnitTest.Logic;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GenerateUnitTest
{
    public static class GenerateUnitTestLogic
    {
        public static List<string> methodNames { get; private set; } = new List<string>();
        //example
        //var resultMockUnitTest = GenerateUnitTestLogic.GenerateTestMethod(MethodBase.GetCurrentMethod(), addOnPaymentConfigs, gracePeriodRequest);
        public static string GenerateTestMethod(this MethodBase methodBase, params object[] requestObjects)
        {
            ClearMethodNames();
            List<string> mockInputFunctions = new List<string>();
            List<string> mockInputMethodNames = new List<string>();
            List<string> assertResults = new List<string>();
            int i = 0;
            foreach (var model in requestObjects)
            {
                string assetResultPattern = string.Empty;

                    string returnTypeName = ((MethodInfo)methodBase).ReturnType.Name;
                    string modelTypeName = model.GetType().Name;
                if (i == requestObjects.Length - 1 && returnTypeName.Equals(modelTypeName))
                {
                    try
                    {
                        assetResultPattern = PostmanLogic.WrtieMockRegression(model).ToString();
                        assertResults.Add(assetResultPattern);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    continue;
                }
                if (model == null)
                {
                    mockInputMethodNames.Add(null);
                }
                else
                {
                    i++;
                    TypeDetail typeDetail = GetTypeName(model);
                    var mockInputUnitTest = GetMockInputUnitTest(model, typeDetail, i);
                    mockInputFunctions.Add(mockInputUnitTest.mockInputFunction);
                    mockInputMethodNames.Add(mockInputUnitTest.mockInputMethodName);
                }

            }
            string mockInputFunction = string.Join(Environment.NewLine, mockInputFunctions);
            string codeMockCallFunction = GetTestMethod(methodBase, mockInputMethodNames);
            codeMockCallFunction = codeMockCallFunction + Environment.NewLine + (string.Join(Environment.NewLine, assertResults));
            string resultCodeMock = $"{mockInputFunction}{Environment.NewLine}{GetTestMethodPattern(methodBase, codeMockCallFunction)}";
            return resultCodeMock;
        }

        private static void ClearMethodNames()
        {
            methodNames = new List<string>();
        }

        private static TypeDetail GetTypeName(object model)
        {
            TypeDetail typeDetail = new TypeDetail();
            if (IsList(model) && IsGenericList(model))
            {
                try
                {
                    Type type = model.GetType().GetGenericArguments().Single();
                    typeDetail.typeName = $"List<{type.Name}>";
                    typeDetail.variableName = $"List{type.Name}";
                }
                catch (Exception)
                {
                    Type type = model.GetType();
                    typeDetail.typeName = $"{type.Name}";
                    typeDetail.variableName = $"{type.Name}";
                }
            }
            else
            {
                Type type = model.GetType();
                typeDetail.typeName = $"{type.Name}";
                typeDetail.variableName = $"{type.Name}";
            }
            return typeDetail;
        }

        public static bool IsList(object value)
        {
            return value is IList || IsGenericList(value);
        }

        public static bool IsGenericList(object value)
        {
            var type = value.GetType();
            return type.IsGenericType && typeof(List<>) == type.GetGenericTypeDefinition();
        }
        private static string GetTestMethodPattern(MethodBase methodBase, string textCalMethod)
        {
            string pattern =
                            "[TestMethod]" + "\r\n" +
                            $"public void {methodBase.Name}_Test()" + "\r\n" +
                            "{" + "\r\n" +
                            $"{textCalMethod}" + "\r\n" +
                            "}";
            return pattern;
        }
        private static string GetTestMethod(MethodBase methodBase, List<string> inputTestMethods)
        {
            string instanceClass = GetInstanceClass(methodBase);
            string paramInputTestMethods = string.Join(" ,", inputTestMethods);
            string pattern = $"var result = {instanceClass}.{methodBase.Name}({paramInputTestMethods});";
            return pattern;
        }

        private static string GetInstanceClass(MethodBase methodBase)
        {
            string className = methodBase.DeclaringType.Name;
            if (methodBase.IsStatic)
            {
                return className;
            }
            else
            {
                return $"new {className}() ";
            }
        }

        private static (string mockInputFunction, string mockInputMethodName) GetMockInputUnitTest(object objects, TypeDetail typeDetail, int i)
        {
            string mockInputMethodName = string.Empty;
            string mockInputFunction = string.Empty;
            string fullName = objects.GetType().FullName;
            bool isModelProject = fullName.Contains(".Model.") || fullName.Contains(".Models.");
            if (!IsList(objects) && !IsGenericList(objects) && !isModelProject)
            {
                if (typeDetail.typeName.ToLower().Equals("string"))
                {
                    mockInputMethodName = "\"" + objects.ToString() + "\"";
                }
                else
                {
                    mockInputMethodName = $"({typeDetail.typeName})" + objects.ToString();
                }
                if (typeof(bool).Name == objects.GetType().Name)
                {
                    mockInputMethodName = objects.ToString().ToLower();
                }
                if (typeof(DateTime).Name == objects.GetType().Name)
                {
                    mockInputMethodName = $"Convert.ToDateTime(\"{objects.ToString()}\")";
                }
            }
            else
            {
                string getMockMethodName = $"GetMock{i}";
                methodNames.Add(mockInputMethodName);
                string codeFirstLine = GetCodeFirstLineMockInput(typeDetail, getMockMethodName);
                string codeSecondLine = GetCodeSecondLineMockInput(objects);
                string codeEndLine = GetCodeEndLineMockInput(typeDetail);
                mockInputFunction = $"{codeFirstLine}{codeSecondLine}{Environment.NewLine}{codeEndLine}{Environment.NewLine}" + "}";
                mockInputMethodName = getMockMethodName + "()";
            }

            return (mockInputFunction, mockInputMethodName);
        }

        private static string GetMockInputMethodName(MethodBase methodBase, TypeDetail typeDetail)
        {
            string mockInputMethodName = $"GetMock{typeDetail.variableName}{methodBase.Name}";
            mockInputMethodName = IfDuplicateMethodNameThenRename(mockInputMethodName);
            return mockInputMethodName;
        }

        private static string GetCodeEndLineMockInput(TypeDetail typeDetail)
        {
            return $"return JsonConvert.DeserializeObject<{typeDetail.typeName}>(raw);";
        }

        private static string GetCodeSecondLineMockInput(object objects)
        {
            string rawJson, rawJsonAfterConvertToTextAssembly;
            try
            {
                rawJson = JsonConvert.SerializeObject(objects);
                rawJsonAfterConvertToTextAssembly = ConvertJsonToText(rawJson);
            }
            catch (Exception)
            {
                rawJsonAfterConvertToTextAssembly = "null";
            }
            string declarRawJson = $"string raw = \"{rawJsonAfterConvertToTextAssembly}\";";
            return declarRawJson;
        }

        private static string GetCodeFirstLineMockInput(TypeDetail typeDetail, string methodName)
        {
            return $"public {typeDetail.typeName} {methodName}(){Environment.NewLine}" + "{";
        }

        private static string IfDuplicateMethodNameThenRename(string methodName)
        {
            if (methodNames.Contains(methodName))
            {
                methodName = methodName + (methodNames.Count(a => methodNames.Contains(methodName)) + 1);
            }
            return methodName;
        }

        private static string ConvertJsonToText(string data)
        {
            var dataList = data.Split('"').ToList();
            return String.Join(@"\" + "\"", dataList.ToArray());
        }

    }
}
