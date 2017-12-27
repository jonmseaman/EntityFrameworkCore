// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Storage.Converters
{
    /// <summary>
    ///     Defines conversions from an object of one type in a model to an object of the same or
    ///     different type in the store.
    /// </summary>
    public class ValueConverter<TModel, TStore> : ValueConverter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueConverter{TModel,TStore}" /> class.
        /// </summary>
        /// <param name="convertToStoreExpression"> An expression to convert objects when writing data to the store. </param>
        /// <param name="convertFromStoreExpression"> An expression to convert objects when reading data from the store. </param>
        /// <param name="mappingHints">
        ///     Hints that can be used by the type mapper to create data types with appropriate
        ///     facets for the converted data.
        /// </param>
        public ValueConverter(
            [NotNull] Expression<Func<TModel, TStore>> convertToStoreExpression,
            [NotNull] Expression<Func<TStore, TModel>> convertFromStoreExpression,
            ConverterMappingHints mappingHints = default)
            : base(
                SanitizeConverter(Check.NotNull(convertToStoreExpression, nameof(convertToStoreExpression))),
                SanitizeConverter(Check.NotNull(convertFromStoreExpression, nameof(convertFromStoreExpression))),
                convertToStoreExpression,
                convertFromStoreExpression,
                mappingHints)
        {
        }

        private static Func<object, object> SanitizeConverter<TIn, TOut>(Expression<Func<TIn, TOut>> convertExpression)
            => v => v == null
                ? (object)null
                : convertExpression.Compile()(Sanitize<TIn>(v));

        private static T Sanitize<T>(object value)
        {
            var unwrappedType = typeof(T).UnwrapNullableType();

            return (T)(unwrappedType != value.GetType()
                    ? Convert.ChangeType(value, unwrappedType)
                    : value);
        }

        /// <summary>
        ///     Gets the expression to convert objects when writing data to the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public new virtual Expression<Func<TModel, TStore>> ConvertToStoreExpression
            => (Expression<Func<TModel, TStore>>)base.ConvertToStoreExpression;

        /// <summary>
        ///     Gets the expression to convert objects when reading data from the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public new virtual Expression<Func<TStore, TModel>> ConvertFromStoreExpression
            => (Expression<Func<TStore, TModel>>)base.ConvertFromStoreExpression;

        /// <summary>
        ///     The CLR type used in the EF model.
        /// </summary>
        public override Type ModelType => typeof(TModel);

        /// <summary>
        ///     The CLR type used when reading and writing from the store.
        /// </summary>
        public override Type StoreType => typeof(TStore);
    }
}
