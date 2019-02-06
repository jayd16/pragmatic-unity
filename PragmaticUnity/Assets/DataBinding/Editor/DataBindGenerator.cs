using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Com.Duffy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public static class DataBindGenerator
{
    
    private static readonly ILogger Log = Debug.unityLogger;

    [InitializeOnLoadMethod]
    private static void AutoGenerate()
    {
        var generatorSettings = ScriptableObject.CreateInstance<DataBindGeneratorSettings>();
        if (!generatorSettings.GenerateOnRecompile) return;
        Generate(generatorSettings);
    }

    private static void Generate(DataBindGeneratorSettings generatorSettings)
    {
        int start = Environment.TickCount;

        var generatedFilesPath = generatorSettings.GeneratedFilesPath;

        foreach (Type type in typeof(DataBindingAttribute).Assembly.GetTypes())
        {
            var bindings = new Dictionary<string, Type>();
            var setBindings = new Dictionary<string, string>();
            var getBindings = new Dictionary<string, string>();
            var invalidateBindings = new Dictionary<string, List<string>>();
            foreach (var memberInfo in type.GetMembers())
            {
                var attrMembers = memberInfo.GetCustomAttributes(typeof(DataBindingAttribute));

                foreach (var attrMember in attrMembers)
                {

                    var id = ((DataBindingAttribute) attrMember).Id;
                    string bindingType = "unknown";
                    var methodInfo = memberInfo as MethodInfo;
                    if (methodInfo != null)
                    {
                        Type paramType;
                        bindings.TryGetValue(id, out paramType);


                        if (methodInfo.ReturnType == typeof(AsyncRequest))
                        {
                            bindingType = "setter";
                            Assert.IsTrue(methodInfo.GetParameters().Length == 1, "bad params on binding");
                            if (paramType == null)
                                paramType = methodInfo.GetParameters()[0].ParameterType;
                            Assert.IsTrue(paramType == methodInfo.GetParameters()[0].ParameterType,
                                "params on bindings do not match");

                            setBindings[id] = methodInfo.Name;
                        }
                        else if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(AsyncRequest<>))
                        {
                            bindingType = "getter";
                            Assert.IsTrue(methodInfo.GetParameters().Length == 0, "bad params on binding");
                            if (paramType == null)
                                paramType = methodInfo.ReturnType.GetGenericArguments()[0];
                            Assert.IsTrue(paramType == methodInfo.ReturnType.GetGenericArguments()[0],
                                "params on bindings do not match");
                            getBindings[id] = methodInfo.Name;
                        }

                        bindings[id] = paramType;

                    }
                    else if (memberInfo is EventInfo)
                    {
                        bindingType = "invalidationEvent";
                        List<string> events;
                        if (!invalidateBindings.TryGetValue(id, out events))
                        {
                            events = new List<string>();
                        }

                        events.Add(memberInfo.Name);
                        invalidateBindings[id] = events;
                    }


                    Log.Log($"{bindingType} binding type for id:{((DataBindingAttribute) attrMember).Id} found on {type}");

                }
            }

            if (bindings.Count < 1) continue;

            
            //ensure path exists
            Directory.CreateDirectory(generatedFilesPath);
            var path = $"{generatedFilesPath}/{type}.cs";

            using (var codeDom = CodeDomProvider.CreateProvider("CSharp"))
            {
                using (var sw = new StreamWriter(path))
                using (var tw = new IndentedTextWriter(sw))
                {
                    /*
                     *
                     * {0} = Binding owner class name
                     * {1} = Binding field declaration
                     * {2) = Binding instantiation and registration
                     * {3} = Dispose
                     *using System.Collections;
                     * public partial class {0}
                     * {{
                     *     {1}
                     * 
                     *     partial void _Awake();
                     *     partial void _OnDestroy();
                     *     private void Awake()
                     *     {{
                     *         _Awake();
                     *         ExposeBindings();
                     *     }}
                     * 
                     *     private void OnDestroy()
                     *     {{
                     *         _OnDestroy();
                     *         DestroyBindings();
                     *     }}
                     * 
                     *     private void ExposeBindings()
                     *     {{
                     *         {2}
                     *     }}
                     * 
                     *     private void DestroyBindings()
                     *     {{
                     *         {3}
                     *     }}
                     * }}"
                     */                 
                    
                    var bindingTypeDeclaration = new CodeTypeDeclaration { Name = type.Name, IsPartial = true};

                    
                    //add some boilerplate 
                    bindingTypeDeclaration.Members.Add(
                        new CodeSnippetTypeMember(
                           "    partial void _Awake();"
                            + "\n    partial void _OnDestroy();"
                            + "\n    private void Awake()"
                            + "\n    {"
                            + "\n        _Awake();"
                            + "\n        ExposeBindings();"
                            + "\n    }"
                            + "\n    private void OnDestroy()"
                            + "\n    {"
                            + "\n        _OnDestroy();"
                            + "\n        DestroyBindings();"
                            + "\n    }")
                        );

                    
                    var exposeBindingsMethod = new CodeMemberMethod()
                    {
                        Name = "ExposeBindings",
                        Attributes = MemberAttributes.Private,
                    };
                    bindingTypeDeclaration.Members.Add(exposeBindingsMethod);
                    var destroyBindings = new CodeMemberMethod()
                    {
                        Name = "DestroyBindings",
                        Attributes = MemberAttributes.Private,
                    };
                    bindingTypeDeclaration.Members.Add(destroyBindings);

                   
                    foreach (var binding in bindings)
                    {
                        var id = binding.Key;
                        var boundType = binding.Value;
                        var bindingType = typeof(DataBinding<>).MakeGenericType(boundType);
                        
                        
                        //CODE GEN: private DataBinding<{1}> _{0}Binding;
                        bindingTypeDeclaration.Members.Add(new CodeMemberField(bindingType, $"_{id}Binding"){Attributes = MemberAttributes.Private});
                        
                        
                        
                        string getter;
                        string setter;
                        getBindings.TryGetValue(id, out getter);
                        setBindings.TryGetValue(id, out setter);
                        /*
                         * code gen prep
                         */
                        var setterExpression = string.IsNullOrEmpty(setter)
                            ? (CodeExpression) new CodePrimitiveExpression(null) 
                            : new CodeMethodReferenceExpression(null, setter);
                        var getterExpression = string.IsNullOrEmpty(getter)
                            ? (CodeExpression) new CodePrimitiveExpression(null) 
                            : new CodeMethodReferenceExpression(null, getter);
                        
                        CodeExpression addListenerExpression;
                        CodeExpression removeListenerExpression;
                        
                        List<string> eventNames;
                        if (invalidateBindings.TryGetValue(id, out eventNames))
                        {
                            var addListenerMethod = new CodeMemberMethod()
                            {
                                Name = $"{id}AddListeners",
                                Attributes = MemberAttributes.Private,
                                Parameters = {new CodeParameterDeclarationExpression(typeof(Action), "l")}
                            };
                            foreach (var eventName in eventNames)
                            {
                                //CODE GEN: eventName += l;
                                addListenerMethod.Statements.Add(new CodeAttachEventStatement(null, eventName,
                                    new CodeVariableReferenceExpression("l")));
                            }
                            
                            var removeListenerMethod = new CodeMemberMethod()
                            {
                                Name = $"{id}RemoveListeners",
                                Attributes = MemberAttributes.Private,
                                Parameters = {new CodeParameterDeclarationExpression(typeof(Action), "l")}
                            };
                            foreach (var eventName in eventNames)
                            {
                                //CODE GEN: eventName += l;
                                removeListenerMethod.Statements.Add(new CodeRemoveEventStatement(null, eventName,
                                    new CodeVariableReferenceExpression("l")));
                            }
                                        
                            addListenerExpression = new CodeMethodReferenceExpression(null, addListenerMethod.Name);
                            removeListenerExpression = new CodeMethodReferenceExpression(null, removeListenerMethod.Name);

                            bindingTypeDeclaration.Members.Add(addListenerMethod);
                            bindingTypeDeclaration.Members.Add(removeListenerMethod);
                        }
                        else
                        {
                            addListenerExpression = new CodePrimitiveExpression(null);
                            removeListenerExpression = new CodePrimitiveExpression(null);
                        }
                        

                        var bindingFieldReference = new CodeFieldReferenceExpression(null, $"_{id}Binding");
                        
                        exposeBindingsMethod.Statements.Add(
                            //CODE GEN:  _{id}Binding = new DataBinding<{bindingType}>(\"{id}\", {setter}, {getter}, null, null);"
                            new CodeAssignStatement(
                                bindingFieldReference,
                                 
                                new CodeObjectCreateExpression(bindingType, 
                                    new CodePrimitiveExpression(id),
                                    setterExpression, 
                                    getterExpression,
                                    addListenerExpression, 
                                    removeListenerExpression)
                            )
                        );
                        
                        //CODE GEN: _{id}Binding.Register();";
                        exposeBindingsMethod.Statements.Add(new CodeMethodInvokeExpression(bindingFieldReference, nameof(DataBinding<object>.Register)));

                        destroyBindings.Statements.Add(new CodeMethodInvokeExpression(bindingFieldReference,nameof(DataBinding<object>.Dispose)));
                        
                        
                        
                        var binderPath = $"{generatedFilesPath}/{id}DataBinder.cs";
                        using (var binderSw = new StreamWriter(binderPath))
                        using (var binderTw = new IndentedTextWriter(binderSw))
                        {
                            /*  public class {id}DataBinder : DataBinder<{boundType}>
                             *  {
                             *      [SerializeField] protected {id}UnityEvent _onGet = new {id}UnityEvent();
                             *      protected override UnityEvent<{bindingType}> OnGet => _onGet;
                             *  
                             *      [Serializable]
                             *      protected class {id}UnityEvent : UnityEvent<{boundType}>
                             *      {
                             *      }
                             *  
                             *  }
                             * 
                             */
                            var dataBinderType = typeof(DataBinder<>).MakeGenericType(binding.Value);
                            //CODE GEN: public class {id}DataBinder : DataBinder<{bindingType}> {}
                            var binderDecl = new CodeTypeDeclaration($"{id}DataBinder")
                            {
                                BaseTypes = {new CodeTypeReference(dataBinderType)}
                            };
                            
                            
                            //CODE GEN: protected override UnityEvent<{bindingType}> OnGet => _onGet; 
                            binderDecl.Members.Add(new CodeMemberProperty()
                            {
                                Name = "OnGet",
                                Attributes = MemberAttributes.Override|MemberAttributes.Family,
                                HasGet = true,
                                HasSet = false,
                                Type = new CodeTypeReference(typeof(UnityEvent<>).MakeGenericType(boundType)),
                                GetStatements = { new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "_onGet"))}
                                
                            });


                            binderDecl.Members.Add(new CodeMemberMethod()
                            {
                                Name = "Reset",
                                Attributes = MemberAttributes.Private,
                                Statements = { new CodeAssignStatement(new CodeFieldReferenceExpression(null, "_dataBindingId"), new CodePrimitiveExpression(id))}
                            });

                            
                            //CODE GEN: [SerializeField] protected {id}UnityEvent _onGet = new {id}UnityEvent();
                            var eventType = new CodeTypeReference($"{id}UnityEvent");
                            binderDecl.Members.Add(
                                new CodeMemberField(eventType, "_onGet")
                                {
                                    Attributes = MemberAttributes.Family,
                                    CustomAttributes =
                                    {
                                        new CodeAttributeDeclaration(
                                            new CodeTypeReference(typeof(SerializeField)))
                                    },
                                    InitExpression = new CodeObjectCreateExpression(eventType)
                                });
                            
                            /*CODE GEN:
                             *  [Serializable]
                             *  protected class {id}UnityEvent : UnityEvent<{bindingType}>
                             *  {
                             *  }
                             */
                            binderDecl.Members.Add(
                                new CodeTypeDeclaration($"{id}UnityEvent")
                                {
                                    Attributes = MemberAttributes.Family, //protected
                                    CustomAttributes =
                                    {
                                        new CodeAttributeDeclaration(
                                            new CodeTypeReference(typeof(SerializableAttribute)))
                                    },
                                    BaseTypes = {new CodeTypeReference(typeof(UnityEvent<>).MakeGenericType(boundType))}
                                }
                            );
                            
                            codeDom.GenerateCodeFromType(binderDecl, binderTw, new CodeGeneratorOptions());
                        }
                    }
                        
                    codeDom.GenerateCodeFromType(bindingTypeDeclaration, tw, new CodeGeneratorOptions());
                    
                }
            }


            Log.Log($"{nameof(DataBindGenerator)} took {Environment.TickCount - start}");
        }
        
    }



}

