﻿namespace Benchmark
{
    using System;

    public interface IActionMapper
    {
        object Map(object source);

        void Map(object source, object destination);
    }

    public interface IActionMapper<in TSource, TDestination> : IActionMapper
    {
        TDestination Map(TSource source);

        void Map(TSource source, TDestination destination);
    }

    public sealed class ActionMapper<TSource, TDestination> : IActionMapper<TSource, TDestination>
    {
        private readonly Func<TDestination> factory;

        private readonly Action<TSource, TDestination>[] actions;

        public ActionMapper(Func<TDestination> factory, Action<TSource, TDestination>[] actions)
        {
            this.factory = factory;
            this.actions = actions;
        }

        // MEMO 本物ではループも展開する
        public TDestination Map(TSource source)
        {
            var destination = factory();
            for (var i = 0; i < actions.Length; i++)
            {
                actions[i](source, destination);
            }

            return destination;
        }

        public void Map(TSource source, TDestination destination)
        {
            for (var i = 0; i < actions.Length; i++)
            {
                actions[i](source, destination);
            }
        }

        public object Map(object source)
        {
            return Map((TSource)source);
        }

        public void Map(object source, object destination)
        {
            Map((TSource)source, (TDestination)destination);
        }
    }

    public sealed class ActionMapperFactory
    {
        private readonly TypePairHashArray<IActionMapper> mappers = new TypePairHashArray<IActionMapper>();

        public void AddMapper(Type sourceType, Type destinationType, IActionMapper mapper)
        {
            // MEMO 本物ではAddのみ(immutableなIFで)
            mappers.AddIfNotExist(sourceType, destinationType, (s, d) => mapper);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source is null)
            {
                return default;
            }

            if (!mappers.TryGetValue(typeof(TSource), typeof(TDestination), out var mapper))
            {
                throw new ArgumentException("Type is not registered.");
            }

            return ((IActionMapper<TSource, TDestination>)mapper).Map(source);
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source is null)
            {
                return;
            }

            if (!mappers.TryGetValue(typeof(TSource), typeof(TDestination), out var mapper))
            {
                throw new ArgumentException("Type is not registered.");
            }

            ((IActionMapper<TSource, TDestination>)mapper).Map(source, destination);
        }

        public TDestination Map<TDestination>(object source)
        {
            if (source is null)
            {
                return default;
            }

            if (!mappers.TryGetValue(source.GetType(), typeof(TDestination), out var mapper))
            {
                throw new ArgumentException("Type is not registered.");
            }

            return (TDestination)mapper.Map(source);
        }

        public void Map(object source, object destination)
        {
            if ((source is null) || (destination is null))
            {
                return;
            }

            if (!mappers.TryGetValue(source.GetType(), destination.GetType(), out var mapper))
            {
                throw new ArgumentException("Type is not registered.");
            }

            mapper.Map(source, destination);
        }
    }
}
