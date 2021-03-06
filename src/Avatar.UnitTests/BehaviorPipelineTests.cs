﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Avatars.UnitTests
{
    public class BehaviorPipelineTests
    {
        [Fact]
        public void WhenInvokingPipelineWithNoBehaviors_ThenInvokesTarget()
        {
            var targetCalled = false;

            var pipeline = new BehaviorPipeline();

            Action a = WhenInvokingPipelineWithNoBehaviors_ThenInvokesTarget;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipelineWithNoBehaviors_ThenTargetCannotInvokeNext()
        {
            var pipeline = new BehaviorPipeline();

            Action a = WhenInvokingPipelineWithNoBehaviors_ThenInvokesTarget;

            Assert.Throws<InvalidOperationException>(() => pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => n().Invoke(m, n))));
        }

        [Fact]
        public void WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { firstCalled = true; return n().Invoke(m, n); }),
                new ExecuteDelegate((m, n) => { secondCalled = true; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget;
            
            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.True(secondCalled);
            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipelineWithNoApplicableBehaviors_ThenInvokesTarget()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new AnonymousBehavior((m, n) => { firstCalled = true; return n().Invoke(m, n); }, m => false),
                new AnonymousBehavior((m, n) => { secondCalled = true; return n().Invoke(m, n); }, m => false));

            Action a = WhenInvokingPipelineWithNoApplicableBehaviors_ThenInvokesTarget;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.False(firstCalled);
            Assert.False(secondCalled);
            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenSkipsNonApplicableBehaviors()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new AnonymousBehavior((m, n) => { firstCalled = true; return n().Invoke(m, n); }),
                new AnonymousBehavior((m, n) => { secondCalled = true; return n().Invoke(m, n); }, m => false));

            Action a = WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.False(secondCalled);
            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorCanShortcircuitInvocation()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { firstCalled = true; return m.CreateValueReturn(null); }),
                new ExecuteDelegate((m, n) => { secondCalled = true; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenBehaviorCanShortcircuitInvocation;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.False(secondCalled);
            Assert.False(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanPassDataWithContext()
        {
            var expected = Guid.NewGuid();
            var actual = Guid.Empty;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { m.Context["guid"] = expected; return n().Invoke(m, n); }),
                new ExecuteDelegate((m, n) => { actual = (Guid)m.Context["guid"]; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenBehaviorsCanPassDataWithContext;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => m.CreateValueReturn(null)));

            Assert.Equal(expected, actual);
            Assert.True(result.Context.ContainsKey("guid"));
            Assert.Equal(expected, result.Context["guid"]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValue()
        {
            var expected = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected)));

            Func<object?> a = NonVoidMethod;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithArg()
        {
            var expected = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object())));

            Func<object, object?> a = NonVoidMethodWithArg;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithRef()
        {
            var expected = new object();
            var output = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), output)));

            NonVoidMethodWithArgRefDelegate a = NonVoidMethodWithArgRef;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(output, result.Outputs[0]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnException()
        {
            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateExceptionReturn(new ArgumentException())));

            Action a = WhenInvokingPipeline_ThenBehaviorsCanReturnException;

            Assert.Throws<ArgumentException>(() 
                => pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()), true));
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithOut()
        {
            var expected = new object();
            var output = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), output)));

            NonVoidMethodWithArgOutDelegate a = NonVoidMethodWithArgOut;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(output, result.Outputs[0]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithRefOut()
        {
            var expected = new object();
            var output = new object();
            var byref = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), byref, output)));

            NonVoidMethodWithArgRefOutDelegate a = NonVoidMethodWithArgRefOut;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, byref, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(byref, result.Outputs[0]);
            Assert.Equal(output, result.Outputs[1]);
        }

        [Fact]
        public void CanExecutePipelineResultNoTarget()
        {
            var value = new object();

            var pipeline = new BehaviorPipeline(new ExecuteDelegate((m, n) => m.CreateValueReturn(value)));

            Func<object?> f = NonVoidMethod;

            Assert.Same(value, pipeline.Execute<object>(new MethodInvocation(this, f.GetMethodInfo())));
        }

        [Fact]
        public void CanExecutePipelineResultWithTarget()
        {
            var value = new object();

            var pipeline = new BehaviorPipeline(new ExecuteDelegate((m, n) => m.CreateValueReturn(value)));

            Func<object?> f = NonVoidMethod;

            Assert.Same(value, pipeline.Execute<object>(
                new MethodInvocation(this, f.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException())));
        }

        [Fact]
        public void CanExecutePipelineWithTarget()
        {
            var pipeline = new BehaviorPipeline();

            Action a = CanExecutePipelineWithTarget;

            pipeline.Execute(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => m.CreateValueReturn(null)));
        }

        [Fact]
        public void CanExecutePipelineNoTarget()
        {
            var pipeline = new BehaviorPipeline(new ExecuteDelegate((m, n) => m.CreateValueReturn(null)));

            Action a = CanExecutePipelineNoTarget;

            pipeline.Execute(new MethodInvocation(this, a.GetMethodInfo()));
        }

        [Fact]
        public void WhenExecutingPipelineWithNoTarget_ThenThrowsIfNoBehaviorReturns()
        {
            var pipeline = new BehaviorPipeline();

            Action a = CanExecutePipelineNoTarget;

            Assert.Throws<NotImplementedException>(() => pipeline.Execute(new MethodInvocation(this, a.GetMethodInfo())));
        }

        [Fact]
        public void WhenExecutingPipeline_ThenBehaviorCanThrow()
        {
            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateExceptionReturn(new ArgumentException())));

            Action a = WhenInvokingPipeline_ThenBehaviorsCanReturnException;

            Assert.Throws<ArgumentException>(()
                => pipeline.Execute(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException())));
        }

        [Fact]
        public void WhenExecutingPipelineResult_ThenBehaviorCanThrow()
        {
            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateExceptionReturn(new ArgumentException())));

            Func<object?> f = NonVoidMethod;

            Assert.Throws<ArgumentException>(()
                => pipeline.Execute<object>(new MethodInvocation(this, f.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException())));
        }

        [Fact]
        public void WhenExecutingPipelineResultWithNoTarget_ThenThrowsIfNoResult()
        {
            var pipeline = new BehaviorPipeline();

            Func<object?> f = NonVoidMethod;

            Assert.Throws<NotImplementedException>(()
                => pipeline.Execute<object>(new MethodInvocation(this, f.GetMethodInfo())));
        }

        [Fact]
        public void WhenSkippingBehavior_ThenBehaviorIsNotExecuted()
        {
            var pipeline = new BehaviorPipeline();

            pipeline.Behaviors.Add(new TestBehavior());

            var invocation = new MethodInvocation(new object(), typeof(object).GetMethod("ToString")!);
            invocation.SkipBehavior<TestBehavior>();

            Assert.Throws<NotImplementedException>(()
                => pipeline.Execute<string>(invocation));
        }

        [Fact]
        public void WhenAddingMultipleBehaviors_ThenCanOptimizeCollectionChanges()
        {
            var changes = new List<NotifyCollectionChangedAction>();

            var pipeline = new BehaviorPipeline();
            ((INotifyCollectionChanged)pipeline.Behaviors).CollectionChanged += (sender, args) => changes.Add(args.Action);

            (pipeline.Behaviors as ISupportInitialize)?.BeginInit();

            pipeline.Behaviors.Add(new TestBehavior());
            pipeline.Behaviors.Add(new TestBehavior());
            pipeline.Behaviors.Add(new TestBehavior());

            Assert.Empty(changes);

            (pipeline.Behaviors as ISupportInitialize)?.EndInit();

            Assert.Single(changes);
            Assert.Equal(NotifyCollectionChangedAction.Reset, changes[0]);
        }

        [Fact]
        public void WhenCloningBehaviors_ThenCanManipulateCopyWithoutAffectingOriginal()
        {
            var changes = new List<NotifyCollectionChangedAction>();

            var pipeline = new BehaviorPipeline();
            pipeline.Behaviors.Add(new TestBehavior());
            pipeline.Behaviors.Add(new TestBehavior());
            pipeline.Behaviors.Add(new TestBehavior());

            var clone = (IList<IAvatarBehavior>)((ICloneable)pipeline.Behaviors).Clone();

            Assert.True(pipeline.Behaviors.SequenceEqual(clone));

            clone.Clear();

            Assert.NotEmpty(pipeline.Behaviors);
            Assert.Empty(clone);
        }

        [Fact]
        public async Task WhenRunningParalell_ThenCanReplaceLocalPipelineFactory()
        {
            var factory1 = new TestBehaviorPipelineFactory();
            var factory2 = new TestBehaviorPipelineFactory();

            await Task.WhenAll(
                Task.Run(() =>
                {
                    BehaviorPipelineFactory.LocalDefault = factory1;
                    Thread.Sleep(50);
                    Assert.Same(factory1, BehaviorPipelineFactory.Default);
                }),
                Task.Run(() =>
                {
                    BehaviorPipelineFactory.LocalDefault = factory2;
                    Thread.Sleep(50);
                    Assert.Same(factory2, BehaviorPipelineFactory.Default);
                })
            );

            Assert.NotSame(factory1, BehaviorPipelineFactory.Default);
            Assert.NotSame(factory2, BehaviorPipelineFactory.Default);
        }

        class TestBehaviorPipelineFactory : IBehaviorPipelineFactory
        {
            public BehaviorPipeline CreatePipeline<TAvatar>()
                => new BehaviorPipeline(new RecordingBehavior());
        }

        class TestBehavior : IAvatarBehavior
        {
            public bool AppliesTo(IMethodInvocation invocation) => true;

            public IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next)
            {
                return invocation.CreateValueReturn(new object());
            }
        }

        delegate object? NonVoidMethodWithArgRefDelegate(object arg1, ref object arg2);
        delegate object? NonVoidMethodWithArgOutDelegate(object arg1, out object arg2);
        delegate object? NonVoidMethodWithArgRefOutDelegate(object arg1, ref object arg2, out object arg3);

        object? NonVoidMethod() => null;
        object? NonVoidMethodWithArg(object arg) => null;
        object? NonVoidMethodWithArgRef(object arg1, ref object arg2) => null;
        object? NonVoidMethodWithArgOut(object arg1, out object arg2) { arg2 = new object (); return null; }
        object? NonVoidMethodWithArgRefOut(object arg1, ref object arg2, out object arg3) { arg3 = new object(); return null; }
    }
}
