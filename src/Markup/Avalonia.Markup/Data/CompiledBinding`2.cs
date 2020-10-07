using System;
using System.Reactive.Disposables;
using Avalonia.Data.Core;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Data
{
    public class CompiledBinding<TIn, TOut> : IBinding<TOut>
        where TIn : class
    {
        /// <summary>
        /// Gets or sets the read function.
        /// </summary>
        public Func<TIn, TOut>? Read { get; set; }

        /// <summary>
        /// Gets or sets the write function.
        /// </summary>
        public Action<TIn, TOut>? Write { get; set; }

        /// <summary>
        /// Gets or sets the links in the binding chain.
        /// </summary>
        public Func<TIn, object>[]? Links { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public Optional<TOut> FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        /// <remarks>
        /// If unset the source is the target control's <see cref="StyledElement.DataContext"/> property.
        /// </remarks>
        public Optional<TIn> Source { get; set; }

        public IDisposable Bind(IAvaloniaObject target, StyledPropertyBase<TOut> property)
        {
            var mode = GetMode(target, property);
            var expression = CreateExpression(target, property, mode);

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, expression, Priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        target.Bind(property, expression, Priority),
                        target.GetBindingObservable(property).Subscribe(expression));
                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        public IDisposable Bind(IAvaloniaObject target, DirectPropertyBase<TOut> property)
        {
            var mode = GetMode(target, property);
            var expression = CreateExpression(target, property, mode);

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, expression, Priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        target.Bind(property, expression, Priority),
                        target.GetBindingObservable(property).Subscribe(expression));
                case BindingMode.OneTime:
                case BindingMode.OneWayToSource:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty property,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(
                this,
                "CompiledBinding was bound using untyped binding mechanism.");

            var mode = GetMode(target, property);
            var expression = CreateExpression(target, property, mode);
            var adapter = new CompiledBindingExpressionAdapter<TOut>(expression, enableDataValidation);
            return new InstancedBinding(adapter, mode, Priority);
        }

        private CompiledBindingExpression<TIn, TOut> CreateExpression(
            IAvaloniaObject target,
            AvaloniaProperty property,
            BindingMode mode)
        {
            if (Read is null)
            {
                throw new InvalidOperationException("Cannot bind CompiledBinding: Read is uninitialized.");
            }

            if (Links is null)
            {
                throw new InvalidOperationException("Cannot bind CompiledBinding: Links is uninitialized.");
            }

            if ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) && Write is null)
            {
                throw new InvalidOperationException($"Cannot bind CompiledBinding {Mode}: Write is uninitialized.");
            }

            var targetIsDataContext = property == StyledElement.DataContextProperty;
            var root = GetRoot(target, property);
            var fallback = FallbackValue;

            // If we're binding to DataContext and our fallback is unset then override the fallback
            // value to null, as broken bindings to DataContext must reset the DataContext in order
            // to not propagate incorrect DataContexts to child controls. See 
            // CompiledBindingTests.DataContext_Binding_Should_Produce_Correct_Results.
            if (targetIsDataContext && !fallback.HasValue)
            {
                fallback = new Optional<TOut>(default);
            }

            return new CompiledBindingExpression<TIn, TOut>(root, Read, Write, Links, fallback);
        }

        private BindingMode GetMode(IAvaloniaObject target, AvaloniaProperty property)
        {
            return Mode == BindingMode.Default ? property.GetMetadata(target.GetType()).DefaultBindingMode : Mode;
        }

        private IObservable<TIn> GetRoot(IAvaloniaObject target, AvaloniaProperty property)
        {
            if (Source.HasValue)
            {
                return ObservableEx.SingleValue(Source.Value);
            }
            else if (property == StyledElement.DataContextProperty)
            {
                return new ParentDataContextRoot<TIn>((IVisual)target);
            }
            else
            {
                return new DataContextRoot<TIn>((IStyledElement)target);
            }
        }
    }
}
