// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage.Converters.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Storage.Converters
{
    /// <summary>
    ///     Defines conversions from an object of one type in a model to an object of the same or
    ///     different type in the store.
    /// </summary>
    public abstract class ValueConverter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueConverter" /> class.
        /// </summary>
        /// <param name="convertToStore">
        ///     The function to convert objects when writing data to the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertFromStore">
        ///     The function to convert objects when reading data from the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertToStoreExpression">
        ///     The expression to convert objects when writing data to the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="convertFromStoreExpression">
        ///     The expression to convert objects when reading data from the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </param>
        /// <param name="mappingHints">
        ///     Hints that can be used by the type mapper to create data types with appropriate
        ///     facets for the converted data.
        /// </param>
        protected ValueConverter(
            [NotNull] Func<object, object> convertToStore,
            [NotNull] Func<object, object> convertFromStore,
            [NotNull] LambdaExpression convertToStoreExpression,
            [NotNull] LambdaExpression convertFromStoreExpression,
            ConverterMappingHints mappingHints = default)

        {
            Check.NotNull(convertToStore, nameof(convertToStore));
            Check.NotNull(convertFromStore, nameof(convertFromStore));
            Check.NotNull(convertToStoreExpression, nameof(convertToStoreExpression));
            Check.NotNull(convertFromStoreExpression, nameof(convertFromStoreExpression));

            ConvertToStore = convertToStore;
            ConvertFromStore = convertFromStore;
            ConvertToStoreExpression = convertToStoreExpression;
            ConvertFromStoreExpression = convertFromStoreExpression;
            MappingHints = mappingHints;
        }

        /// <summary>
        ///     Gets the function to convert objects when writing data to the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual Func<object, object> ConvertToStore { get; }

        /// <summary>
        ///     Gets the function to convert objects when reading data from the store,
        ///     setup to handle nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual Func<object, object> ConvertFromStore { get; }

        /// <summary>
        ///     Gets the expression to convert objects when writing data to the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual LambdaExpression ConvertToStoreExpression { get; }

        /// <summary>
        ///     Gets the expression to convert objects when reading data from the store,
        ///     exactly as supplied and may not handle
        ///     nulls, boxing, and non-exact matches of simple types.
        /// </summary>
        public virtual LambdaExpression ConvertFromStoreExpression { get; }

        /// <summary>
        ///     The CLR type used in the EF model.
        /// </summary>
        public abstract Type ModelType { get; }

        /// <summary>
        ///     The CLR type used when reading and writing from the store.
        /// </summary>
        public abstract Type StoreType { get; }

        /// <summary>
        ///     Hints that can be used by the type mapper to create data types with appropriate
        ///     facets for the converted data.
        /// </summary>
        public virtual ConverterMappingHints MappingHints { get; }

        /// <summary>
        ///     Checks that the type used with a value converter is supported by that converter and throws if not.
        /// </summary>
        /// <param name="type"> The type to check. </param>
        /// <param name="converterType"> The value converter type. </param>
        /// <param name="supportedTypes"> The types that are supported. </param>
        /// <returns> The given type. </returns>
        protected static Type CheckTypeSupported(
            [NotNull] Type type,
            [NotNull] Type converterType,
            [NotNull] params Type[] supportedTypes)
        {
            Check.NotNull(type, nameof(type));
            Check.NotNull(converterType, nameof(converterType));
            Check.NotEmpty(supportedTypes, nameof(supportedTypes));

            if (!supportedTypes.Contains(type))
            {
                throw new InvalidOperationException(
                    CoreStrings.ConverterBadType(
                        converterType.ShortDisplayName(),
                        type.ShortDisplayName(),
                        string.Join(", ", supportedTypes.Select(t => t.ShortDisplayName()))));
            }

            return type;
        }

        /// <summary>
        ///     Composes another <see cref="ValueConverter" /> instance with this one such that
        ///     the result of the first conversion is used as the input to the second conversion.
        /// </summary>
        /// <param name="secondConverter"> The second converter. </param>
        /// <returns> The composed converter. </returns>
        public virtual ValueConverter ComposeWith(
            [CanBeNull] ValueConverter secondConverter)
        {
            if (secondConverter == null)
            {
                return this;
            }

            if (StoreType.UnwrapNullableType() != secondConverter.ModelType.UnwrapNullableType())
            {
                throw new ArgumentException(
                    CoreStrings.ConvertersCannotBeComposed(
                        ModelType.ShortDisplayName(),
                        StoreType.ShortDisplayName(),
                        secondConverter.ModelType.ShortDisplayName(),
                        secondConverter.StoreType.ShortDisplayName()));
            }

            var firstConverter
                = StoreType.IsNullableType()
                  && !secondConverter.ModelType.IsNullableType()
                    ? ComposeWith(
                        (ValueConverter)Activator.CreateInstance(
                            typeof(CastingConverter<,>).MakeGenericType(
                                StoreType,
                                secondConverter.ModelType),
                            MappingHints))
                    : this;

            return (ValueConverter)Activator.CreateInstance(
                typeof(CompositeValueConverter<,,>).MakeGenericType(
                    firstConverter.ModelType,
                    firstConverter.StoreType,
                    secondConverter.StoreType),
                firstConverter,
                secondConverter,
                firstConverter.MappingHints.With(secondConverter.MappingHints));
        }
    }
}
