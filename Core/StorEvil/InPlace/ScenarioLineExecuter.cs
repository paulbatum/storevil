using System;
using System.Diagnostics;
using System.Reflection;
using StorEvil.Context;
using StorEvil.Core;
using StorEvil.Events;
using StorEvil.Interpreter;

namespace StorEvil.InPlace
{
    [DebuggerStepThrough]
    public class ScenarioLineExecuter
    {
        public ScenarioLineExecuter(ScenarioInterpreter scenarioInterpreter,
                                    IEventBus eventBus)
        {
            _memberInvoker = new MemberInvoker();
           
            _scenarioInterpreter = scenarioInterpreter;
            _eventBus = eventBus;
        }

        private readonly MemberInvoker _memberInvoker;
        private readonly ScenarioInterpreter _scenarioInterpreter;
        private readonly IEventBus _eventBus;
        private object _lastResult;
        private readonly ImplementationHelper _implementationHelper = new ImplementationHelper();

        
        public LineStatus ExecuteLine(Scenario scenario, ScenarioContext storyContext, string line)
        {
            DebugTrace.Trace("ScenarioLineExecter.ExecuteLine", line);
            InvocationChain chain = GetMatchingChain(storyContext, line);

            if (chain == null)
            {
                var suggestion = _implementationHelper.Suggest(line);

                _eventBus.Raise(new LinePending {Scenario = scenario, Line = line, Suggestion = suggestion });
                return LineStatus.Pending;
            }

            if (!ExecuteChain(scenario, storyContext, chain, line))
                return LineStatus.Failed;

            _eventBus.Raise(new LinePassed { Scenario = scenario, Line = line });

            return LineStatus.Passed;
        }

        
        private bool ExecuteChain(Scenario scenario, ScenarioContext storyContext, InvocationChain chain, string line)
        {
            string successPart = "";
            _lastResult = null;
            foreach (var invocation in chain.Invocations)
            {
                try
                {
                    InvokeContextMember(storyContext, invocation);
                    successPart += invocation.MatchedText + " ";
                }
                catch (TargetInvocationException ex)
                {
                   
                    if (ex.InnerException is ScenarioPendingException)
                    {
                        _eventBus.Raise(new LinePending { Scenario = scenario, Line = line });
                    }
                    else
                    {
                        _eventBus.Raise(new LineFailed
                        {
                            Scenario = scenario,
                            Line = line,                          
                            SuccessPart = successPart.Trim(),
                            FailedPart = invocation.MatchedText, 
                            ExceptionInfo = GetExceptionMessage(ex)
                        });
                    }

                    return false;
                }
            }

            return true;
        }

        private void InvokeContextMember(ScenarioContext scenarioContext, Invocation invocation)
        {
            MemberInfo info = invocation.MemberInfo;
            var declaringType = info.DeclaringType;
            var context = _lastResult ?? scenarioContext.GetContext(declaringType);
            _lastResult = _memberInvoker.InvokeMember(info, invocation.ParamValues, context);
        }

        
        private InvocationChain GetMatchingChain(ScenarioContext storyContext, string line)
        {
            var chain = _scenarioInterpreter.GetChain(storyContext, line);

            return chain;
        }

        
        private static string GetExceptionMessage(Exception exception)
        {
            var ex = exception.InnerException ?? exception;

            var noStackTrace = false; // ex.GetType().Name.Contains("Assertion");

            return noStackTrace ? ex.Message : ex.Message + "\r\n" + ex;
        }
    }

    public enum LineStatus
    {
        Passed,
        Failed,
        Pending
    }
}