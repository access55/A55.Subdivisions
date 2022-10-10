﻿using A55.Subdivisions.Aws.Hosting;

namespace A55.Subdivisions.Aws.Tests.Builders;

internal class ConsumerDescriberBuilder
{
    string topicName = "good_name";
    TimeSpan? pollingInterval = TimeSpan.Zero;
    int? maxConcurrency = 1;
    Type messageType = typeof(TestMessage);
    Type consumerType = typeof(TestConsumer);
    Func<Exception, Task>? errorHandler;

    public ConsumerDescriberBuilder UsingConsumer<TConsumer, TMessage>() where TConsumer : IConsumer<TMessage>
        where TMessage : notnull =>
        WithConsumerType<TConsumer>()
            .WithMessageType<TMessage>();

    public ConsumerDescriberBuilder WithValidConsumerType<T>() where T : IWeakConsumer =>
        WithConsumerType<T>();

    public ConsumerDescriberBuilder WithConsumerType<T>() =>
        WithConsumerType(typeof(T));

    public ConsumerDescriberBuilder WithConsumerType(Type type)
    {
        consumerType = type;
        return this;
    }

    public ConsumerDescriberBuilder WithMessageType<T>() =>
        WithMessageType(typeof(T));

    public ConsumerDescriberBuilder WithMessageType(Type type)
    {
        messageType = type;
        return this;
    }

    public ConsumerDescriberBuilder WithTopicName(string name)
    {
        topicName = name;
        return this;
    }

    public ConsumerDescriberBuilder WithConcurrency(int max)
    {
        maxConcurrency = max;
        return this;
    }

    public ConsumerDescriberBuilder WithPolling(TimeSpan interval)
    {
        pollingInterval = interval;
        return this;
    }

    public ConsumerDescriberBuilder WithErrorHandler(Func<Exception, Task> handler)
    {
        errorHandler = handler;
        return this;
    }

    public IConsumerDescriber Generate()
    {
        var value = A.Fake<IConsumerDescriber>();
        A.CallTo(() => value.TopicName).Returns(topicName);
        A.CallTo(() => value.ConsumerType).Returns(consumerType);
        A.CallTo(() => value.MessageType).Returns(messageType);
        A.CallTo(() => value.MaxConcurrency).Returns(maxConcurrency);
        A.CallTo(() => value.PollingInterval).Returns(pollingInterval);
        A.CallTo(() => value.ErrorHandler).Returns(errorHandler);
        return value;
    }
}
