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
        //exaple
        //var resultMockUnitTest = GenerateUnitTestLogic.GenerateTestMethod(MethodBase.GetCurrentMethod(), addOnPaymentConfigs, gracePeriodRequest);
        public static string GenerateTestMethod(this MethodBase methodBase, params object[] requestObjects)
        {
            ClearMethodNames();
            List<string> mockInputFunctions = new List<string>();
            List<string> mockInputMethodNames = new List<string>();

            foreach (var model in requestObjects)
            {
                if (model == null)
                {
                    mockInputMethodNames.Add(null);
                }
                else
                {
                    TypeDetail typeDetail = GetTypeName(model);
                    var mockInputUnitTest = GetMockInputUnitTest(methodBase, model, typeDetail);
                    mockInputFunctions.Add(mockInputUnitTest.mockInputFunction);
                    mockInputMethodNames.Add(mockInputUnitTest.mockInputMethodName);
                }

            }
            string mockInputFunction = string.Join(Environment.NewLine, mockInputFunctions);
            string codeMockCallFunction = GetTestMethod(methodBase, mockInputMethodNames);
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

        private static (string mockInputFunction, string mockInputMethodName) GetMockInputUnitTest(MethodBase methodBase, object objects, TypeDetail typeDetail)
        {
            string mockInputMethodName = GetMockInputMethodName(methodBase, typeDetail);
            methodNames.Add(mockInputMethodName);
            string codeFirstLine = GetCodeFirstLineMockInput(typeDetail, mockInputMethodName);
            string codeSecondLine = GetCodeSecondLineMockInput(objects);
            string codeEndLine = GetCodeEndLineMockInput(typeDetail);
            string mockInputFunction = $"{codeFirstLine}{codeSecondLine}{Environment.NewLine}{codeEndLine}{Environment.NewLine}" + "}";
            return (mockInputFunction, $"{mockInputMethodName}()");
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
