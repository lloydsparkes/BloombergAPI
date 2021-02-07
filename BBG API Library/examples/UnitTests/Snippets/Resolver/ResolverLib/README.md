
# Resolver Test Examples

There are several interactions that are particular to resolvers:
 - Resolver service registration
 - Permission requests

## Mocking IProviderSession

A mocked `IProviderSession` can then be used in place of a real instance to
set expectations.

For example, given an event handler, `MyEventHandler`, it can be tested as
the following:

```c#
Mock<IProvierSession> mockSession;

[Test]
public void FirstTest() {
    MyEventHandler testHandler = ...;
    Event testEvent = ...; // See next section

    // Setup mock
    this.mockSession.SetUp(x => x.RegisterService(
            It.IsAny<string>(),
            It.IsAny<Identity>(),
            It.IsAny<ServiceRegistrationOptions>()))
        .Returns(true)
        .Verifiable();

    testHandler.processEvent(testEvent, this.mockSession);

    // Verify mock
    this.mockSession.VerifyAll();
}
```

## Creating Test Events

In order to be able to test `EventHandler`s or the output of
`session.NextEvent()`, the application should be able to generate custom
test events / messages.

Some samples are provided in this package to demonstrate how to generate
all possible admin messages.
