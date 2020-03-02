using System;

namespace RabbitMqEventBus.Base
{
    public class SubscriptionInfo
    {
        private SubscriptionInfo(Boolean isDynamic, Type handlerType)
        {
            IsDynamic = isDynamic;
            HandlerType = handlerType;
        }

        public Boolean IsDynamic { get; }
        public Type HandlerType { get; }

        public static SubscriptionInfo Dynamic(Type handlerType)
        {
            return new SubscriptionInfo(true, handlerType);
        }

        public static SubscriptionInfo Typed(Type handlerType)
        {
            return new SubscriptionInfo(false, handlerType);
        }
    }
}