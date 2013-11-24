﻿using System;

namespace Regalo.Core
{
    public class MessageHandlerContext<TEntity> : IMessageHandlerContext<TEntity> 
        where TEntity : AggregateRoot, new()
    {
        private readonly IRepository<TEntity> _repository;
        private readonly IEventBus _eventBus;

        public MessageHandlerContext(IRepository<TEntity> repository, IEventBus eventBus)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (eventBus == null) throw new ArgumentNullException("eventBus");

            _repository = repository;
            _eventBus = eventBus;
        }

        public TEntity Get(Guid id)
        {
            return _repository.Get(id);
        }

        public void SaveAndPublishEvents(TEntity entity)
        {
            var uncommittedEvents = entity.GetUncommittedEvents();
            _repository.Save(entity);
            _eventBus.Publish(uncommittedEvents);
        }
    }
}