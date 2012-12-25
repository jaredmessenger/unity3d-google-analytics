unity3d-google-analytics
========================

Track players and events using Google Analytics

Setup
-----
* Add GoogleAnalytics.cs to your master object
* Add your property id provided by google
* Add your default url

Track Level
-------------
```CSharp
GALevel level = new GALevel();
GoogleAnalytics.instance.Add(level);
GoogleAnalytics.instance.Dispatch();
```

Track Event
-----------
```CSharp
GAEvent myEvent = new GAEvent("MyCategory", "MyAction");
GoogleAnalytics.instance.Add(myEvent);
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


TODO
----
* Set Custom Variables 8(data)9(data)11(data)
* Track E-Commerce Purchase data

