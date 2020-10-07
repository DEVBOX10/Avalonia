﻿using System;
using System.Linq.Expressions;
using Avalonia.Data.Core.Parsers;

#nullable enable

namespace Avalonia.Data
{
    /// <summary>
    /// Provides factory methods for creating <see cref="CompiledBinding{TIn, TOut}"/> objects from
    /// C# lambda expressions.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding.</typeparam>
    public static class CompiledBinding<TIn>
        where TIn : class
    {
        public static CompiledBinding<TIn, TOut> Default<TOut>(
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write)
        {
            return new CompiledBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Write = write,
                Links = ExpressionChainVisitor<TIn>.Build(read),
                Mode = BindingMode.Default,
            };
        }

        public static CompiledBinding<TIn, TOut> OneWay<TOut>(Expression<Func<TIn, TOut>> read)
        {
            return new CompiledBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Links = ExpressionChainVisitor<TIn>.Build(read),
            };
        }

        public static CompiledBinding<TIn, TOut> TwoWay<TOut>(
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write)
        {
            return new CompiledBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Write = write,
                Links = ExpressionChainVisitor<TIn>.Build(read),
                Mode = BindingMode.TwoWay,
            };
        }

        public static CompiledBinding<TIn, TOut> OneTime<TOut>(Expression<Func<TIn, TOut>> read)
        {
            return new CompiledBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Links = ExpressionChainVisitor<TIn>.Build(read),
                Mode = BindingMode.OneTime,
            };
        }
    }
}
