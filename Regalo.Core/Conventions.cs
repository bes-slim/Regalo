﻿using System;

namespace Regalo.Core
{
    public class Conventions
    {
        private static bool _aggregatesMustImplementApplyMethods = false;
        private static string _aggregateIdPropertyName = "AggregateId";
        private static Func<Type, Type> _findAggregateTypeForEventType = null;

        public static string AggregateIdPropertyName { get { return _aggregateIdPropertyName; } }
        public static bool AggregatesMustImplementApplyMethods { get { return _aggregatesMustImplementApplyMethods; } }
        public static Func<Type, Type> FindAggregateTypeForEventType { get { return _findAggregateTypeForEventType; } }

        public static void SetAggregateIdPropertyName(string value)
        {
            _aggregateIdPropertyName = value;
        }

        public static void SetAggregatesMustImplementApplymethods(bool value)
        {
            _aggregatesMustImplementApplyMethods = value;
        }

        public static void SetFindAggregateTypeForEventType(Func<Type, Type> findAggregateTypeForEventType)
        {
            _findAggregateTypeForEventType = findAggregateTypeForEventType;
        }
    }
}