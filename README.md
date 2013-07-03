unity3d-google-analytics
========================

Track players and events using Google Analytics

Setup
-----
* Add GoogleAnalytics.cs to your master object
* Add your property id provided by google (example: UA-XXX-X)
* Add your default url (example: MySite.com)

### Checkout the [WIKI](https://github.com/jared-mess/unity3d-google-analytics/wiki) for an example on how to create a Google Analytics profile.

Track Level
-------------
```CSharp
GALevel level = new GALevel();
// Add the level to the save queue
GoogleAnalytics.instance.Add(level);
// Upload ALL the items in the save queue to Google
GoogleAnalytics.instance.Dispatch();
```

Track Event
-----------
```CSharp
GAEvent myEvent = new GAEvent("MyCategory", "MyAction");
GoogleAnalytics.instance.Add(myEvent);
GoogleAnalytics.instance.Dispatch();

// You can also add a label
GAEvent myEventWithLabel = new GAEvent("MyCategory", "MyAction", "MyLabel");
GoogleAnalytics.instance.Add(myEventWithLabel);
GoogleAnalytics.instance.Dispatch();

// You can also add a value too
GAEvent myEventWithLabelAndValue = new GAEvent("MyCategory", "MyAction", "MyLabel", 5);
GoogleAnalytics.instance.Add(myEventWithLabelAndValue);
GoogleAnalytics.instance.Dispatch();
```

Track User Timing
-----------------
```CSharp
GAUserTimer myTimer = new GAUserTimer("MyTimerCategory", "MyTimerVariable");
myTimer.Start();
// Do a bunch of stuff...
myTimer.Stop();
GoogleAnalytics.instance.Add(myTimer);
GoogleAnalytics.instance.Dispatch();
```

Track E-Commerce
----------------
```CSharp
// The price is a decimal type!  Don't forget to add the "m" after the numbers.
GAPurchaseItem purchasedItem = new GAPurchaseItem("MySku or ProductId", "MyProductName", 7.99m);
// Add the item to the queue
GoogleAnalytics.instance.Add(purchasedItem);
// Upload the queue
GoogleAnalytics.instance.Dispatch();
````

TODO
----
* Set Custom Variables 8(data)9(data)11(data)

