using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoogleAnalytics : MonoBehaviour {
	
	public string propertyID;
	public string defaultURL;
	
	public static GoogleAnalytics instance;
	
	private Hashtable sessionRequestParams = new Hashtable();
	
	private Queue<Hashtable> requestQueue  = new Queue<Hashtable>();
	
	private string currentSessionStartTime;
	private string lastSessionStartTime;
	private string firstSessionStartTime;
	private int sessions;
	
	private bool dispatchInProgress = new bool();
	
	void Awake()
	{
		if(instance)
			DestroyImmediate(gameObject);
		else
		{
			DontDestroyOnLoad(gameObject);
			instance = this;
		}
	}
	
	public void Start(){
		// Increment the number of times played
		IncrSessions();
		
		//Get the player prefs last time played and current time
		currentSessionStartTime = GetEpochTime().ToString();
		lastSessionStartTime = SavedLastSessionStartTime;
		firstSessionStartTime = SavedFirstSessionStartTime;
		sessions = NumSessions;
		
		// Set the last session start time
		SavedLastSessionStartTime = currentSessionStartTime;
		
		string screenResolution = Screen.width.ToString() + "x" + Screen.height.ToString();
		
		sessionRequestParams["utmac"] = propertyID;
		sessionRequestParams["utmhn"] = SystemInfo.deviceType.ToString();
		sessionRequestParams["utmfl"] = Application.unityVersion.ToString();	
		sessionRequestParams["utmsc"] = "24-bit";
		sessionRequestParams["utmsr"] = screenResolution;
		sessionRequestParams["utmwv"] = "4.9.1";
		sessionRequestParams["utmul"] = "en-us";
		sessionRequestParams["utmcs"] = "ISO-8859-1";
		sessionRequestParams["utmcn"] = "1";
	}
	
	private Hashtable LevelSpecificRequestParams()
	{
		// Copy the session request params
		Hashtable levelRequestParams = new Hashtable(sessionRequestParams);
		
		// Page Title
		levelRequestParams["utmdt"] = System.Uri.EscapeDataString( Application.loadedLevelName );
		
		// Will be overridden if you use GALevel (Page)
		levelRequestParams["utmp"]  = System.Uri.EscapeDataString( Application.loadedLevelName );
		
		return levelRequestParams;
	}
	
	public void Add(GALevel gaLevel)
	{
		Hashtable eventSpecificParams = (Hashtable)LevelSpecificRequestParams().Clone();
		
		eventSpecificParams["utmcc"] = CookieData();
		eventSpecificParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		eventSpecificParams["utmp"]  = gaLevel.ToUrlParamString();
		
		requestQueue.Enqueue(eventSpecificParams);
	}
	
	public void Add(GAEvent gaEvent)
	{
		Hashtable eventSpecificParams = (Hashtable)LevelSpecificRequestParams().Clone();
		
		eventSpecificParams["utmt"]  = GoogleTrackTypeToString( GoogleTrackType.GAEvent );
		eventSpecificParams["utmcc"] = CookieData();
		eventSpecificParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		eventSpecificParams["utme"]  = gaEvent.ToUrlParamString();
		
		if (gaEvent.NonInteraction)
		{
			eventSpecificParams["utmni"] = 1;	
		}
		
		if (gaEvent.Level != null)
		{
			eventSpecificParams["utmp"]  = gaEvent.Level.ToUrlParamString();
		}
		
		requestQueue.Enqueue(eventSpecificParams);
	}
	
	//
	// You can purchase items not tied to a transaction, Google will create a blank transaction
	//
	public void Add(GAPurchaseItem gaPurchaseItem)
	{
		Hashtable eventSpecificParams = (Hashtable)LevelSpecificRequestParams().Clone();
		
		eventSpecificParams["utmt"]  = GoogleTrackTypeToString( GoogleTrackType.GAPurchaseItem );
		eventSpecificParams["utmcc"] = CookieData();
		eventSpecificParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		
		// Purchase specific params
		Hashtable purchaseParams = gaPurchaseItem.ToParamHashtable();
		foreach(DictionaryEntry item in purchaseParams)
		{
			eventSpecificParams[item.Key] = item.Value;	
		}
		
		requestQueue.Enqueue(eventSpecificParams);
	}
	
	//
	//  For more info on user tracking https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingTiming
	//
	public void Add(GAUserTimer gaUserTimer)
	{
	 	
		Hashtable eventSpecificParams = (Hashtable)LevelSpecificRequestParams().Clone();
		
		eventSpecificParams["utmt"] = GoogleTrackTypeToString( GoogleTrackType.GATiming );
		eventSpecificParams["utmn"] = Random.Range(1000000000,2000000000).ToString();
		eventSpecificParams["utmcc"] = CookieData();
		eventSpecificParams["utme"]  = gaUserTimer.ToUrlParamString();
		
		if (gaUserTimer.Level != null)
		{
			eventSpecificParams["utmp"]  = gaUserTimer.Level.ToUrlParamString();
		}
		
		requestQueue.Enqueue(eventSpecificParams);
	}
	
	//
	// turns all the events into request urls
	//
	public void Dispatch()
	{
		if ((requestQueue.Count > 0) && !dispatchInProgress)
		{
			StartCoroutine(DelayedDispatch());
		}
	}
	
	//
	// Dispatches all the requests
	//
	IEnumerator DelayedDispatch()
	{
		dispatchInProgress = true;
			
		yield return new WaitForEndOfFrame();
		
		while (requestQueue.Count > 0)
		{
			Hashtable eventParams = (Hashtable)requestQueue.Dequeue();
			string urlRequestParams = BuildRequestString(eventParams);
			string url = "http://www.google-analytics.com/__utm.gif?" + urlRequestParams;
			yield return StartCoroutine( MakeRequest(url, eventParams) );
		}
		
		dispatchInProgress = false;
	}
	
	
	//
	//  send the request to google
	//
	IEnumerator MakeRequest(string url, Hashtable evt)
	{
		// HACK: make it think it's an image to get by crossdomain issues
#if UNITY_WEBPLAYER
		if (url.Contains("?")) {
			url += "&";
		}
		else {
			url += "?";
		}
		url += "_fakeext=.jpg";
#endif

		WWW request = new WWW(url);

		yield return request;

		if(request.error == null)
		{
            Debug.Log("Google Analytic Request Sent");
		}else{
			Debug.LogWarning("GoogleAnalytics WWW failure: " + request.error.ToString());	
		}
	}

	
	private string GoogleTrackTypeToString(GoogleTrackType trackType)
	{
		switch(trackType)
		{
		case GoogleTrackType.GAEvent:
			return "event";
		case GoogleTrackType.GALevel:
			return "page";
		case GoogleTrackType.GAPurchaseItem:
			return "item";
		case GoogleTrackType.GATiming:
			return "event";
		default:
			return "page";
		}
	}
	
	private long DeviceIdentifier
	{
        get{ return Hash (SystemInfo.deviceUniqueIdentifier ); }
	}
	
	private int NumSessions
	{
		get{ return PlayerPrefs.GetInt("gaNumSessions"); }
	}
	
	private void IncrSessions()
	{
		int sessions = PlayerPrefs.GetInt("gaNumSessions");
		sessions += 1;
		PlayerPrefs.SetInt("gaNumSessions", sessions);
	}
	
	private string SavedFirstSessionStartTime
	{
		get{ if (PlayerPrefs.HasKey("gaFirstSessionStartTime"))
			{
				return PlayerPrefs.GetString("gaFirstSessionStartTime");
			}else{
				long currentTime = GetEpochTime();
				PlayerPrefs.SetString("gaFirstSessionStartTime", currentTime.ToString());
				return PlayerPrefs.GetString("gaFirstSessionStartTime");
			}
		}
	}
	
	private string SavedLastSessionStartTime
	{
		get{ 
			if (PlayerPrefs.HasKey("gaLastSessionStartTime"))
			{
				return PlayerPrefs.GetString("gaLastSessionStartTime"); 
			}else{
				string firstSession = SavedFirstSessionStartTime;
				PlayerPrefs.SetString("gaLastSessionStartTime", firstSession);
				return firstSession;
			}
		}
		set{ PlayerPrefs.SetString("gaLastSessionStartTime", value.ToString()); }
	}
	
	// Grab the cookie data for every event/pageview because it grabs the current time
	private string CookieData()
	{
		long domainHash   = Hash(defaultURL);
		
		// __utma Identifies unique Visitors
		// <Domain hash>.<visitor id>.<time of initial session>.<time of previsous session>.<time of current session>.<session number>
		string _utma   = domainHash + "." + DeviceIdentifier + "." + firstSessionStartTime + "." + 
			lastSessionStartTime + "." + currentSessionStartTime + "." + sessions + WWW.EscapeURL(";") + WWW.EscapeURL("+");

		// __utmz Referral information in the cookie
		string cookieUTMZstr = "utmcsr" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmccn" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmcmd" + WWW.EscapeURL("=") + "(none)" + WWW.EscapeURL(";");
		
		string _utmz = domainHash + "." + currentSessionStartTime + "." + sessions + ".1." + cookieUTMZstr;
		
		return "__utma" + WWW.EscapeURL("=") + _utma + "__utmz" + WWW.EscapeURL("=") + _utmz;
	}
	
	private string BuildRequestString(Hashtable urlParams)
	{
		List<string> args = new List<string>();
		foreach( string key in urlParams.Keys ) 
		{
			args.Add( key + "=" + urlParams[key] );	
		}
		return string.Join("&", args.ToArray());	
	}
	
	private long Hash(string url)
	{
		if(url.Length < 3) return Random.Range(10000000,99999999);
		
		int hash = 0;
		int hashCmp = 0;
		for(int urlLen=url.Length-1; urlLen>=0; urlLen--)
		{
			int charCode = (int)url[urlLen];
		    hash    = (hash<<6&268435455) + charCode + (charCode<<14);
		    hashCmp = hash&266338304;
		    hash    = hashCmp != 0 ? hash^hashCmp>>21 : hash;
		}
		return hash;
	}
	
	public long GetEpochTime() 
	{
		System.DateTime currentTime = System.DateTime.Now;
		System.DateTime epochStart  = System.Convert.ToDateTime("1/1/1970 0:00:00 AM");
		System.TimeSpan timeSpan    = currentTime.Subtract(epochStart);
		
		long epochTime = ((((((timeSpan.Days * 24) + timeSpan.Hours) * 60) + timeSpan.Minutes) * 60) + timeSpan.Seconds);
		
		return epochTime;
	}
}

public enum GoogleTrackType{
	GALevel,
	GAEvent,
	GAPurchaseItem,
	GATiming,
}

public class GALevel
{
	private string _page_name;
	
	public GALevel ()
	{
		_page_name = Application.loadedLevelName;	
	}
	
	public GALevel (string levelName)
	{
		_page_name = levelName;	
	}
	
	
	public string Level
	{
		get{ return _page_name; }
		set{ _page_name = value; }
	}
	
	public string ToUrlParamString()
	{
		if (Level == null)
		{
			throw new System.ArgumentException("GALevel: Please Specify a Level Name");	
		}
		return  System.Uri.EscapeDataString( Level );
	}
}

public class GAEvent
{
	private GALevel _level;
	private string _category;
	private string _action;
	private string _opt_label;
	private int _opt_value = -1;
	private bool _opt_noninteraction = false;
	
	public GAEvent(string category, string action)
	{
		Category = category;
		Action   = action;
	}
	
	public GAEvent(string category, string action, string label)
	{
		Category = category;
		Action   = action;
		Label    = label;
	}
	
	public GAEvent(string category, string action, string label, int opt_value)
	{
		Category = category;
		Action   = action;
		Label    = label;
		Value    = opt_value;
	}
	
	public GAEvent(GALevel level, string category, string action, string label)
	{
		Level    = level;
		Category = category;
		Action   = action;
		Label    = label;
	}
	
	public GAEvent(GALevel level, string category, string action, string label, int opt_value)
	{
		Level    = level;
		Category = category;
		Action   = action;
		Label    = label;
		Value    = opt_value;
	}
	
	public GALevel Level
	{
		get { return _level; }
		set { _level = value; }
	}
	
	public string Category
	{
		get{ return _category;  }
		set{ _category = value; }
	}
	
	public string Action
	{
		get{ return _action;}
		set{ _action = value;}
	}
	
	public string Label
	{
		get{ return _opt_label; }
		set{ _opt_label = value; }
	}
	
	public int Value
	{
		get{ return _opt_value; }
		set{ _opt_value = value; }
	}
	
	public bool NonInteraction
	{
		get{ return _opt_noninteraction;}
		set{ _opt_noninteraction = value; }
	}
	
	public string ToUrlParamString()
	{
		//"5(<category>*<action>*<label>*<value>)"
		string utme = "5(";
		utme += System.Uri.EscapeDataString( Category );
		utme += "*" + System.Uri.EscapeDataString( Action );
		if (Category == null || Action == null)
		{
			throw new System.ArgumentException("GAEvent: Category and Action must be specified");	
		}
		if (Label != null)
		{
			utme += "*" + System.Uri.EscapeDataString( Label );
		}
		
		if (Value != -1)
		{
			utme += ")(" + Value;
		}
		
		utme += ")";
			
		return utme;	
	}
}


public class GAUserTimer
{
	private GALevel _level;
	private string _category;
	private string _variable;
	private string _label;
	
	private long _startTime;
	private long _stopTime;
	
	public GAUserTimer(string category, string variable)
	{
		Category = category;
		Variable = variable;
	}
	
	public GAUserTimer(string category, string variable, string label)
	{
		Category = category;
		Variable = variable;
		Label    = label;
	}
	
	public GAUserTimer(GALevel level, string category, string variable)
	{
		Level    = level;
		Category = category;
		Variable = variable;
	}
	
	public GAUserTimer(GALevel level, string category, string variable, string label)
	{
		Level    = level;
		Category = category;
		Variable = variable;
		Label    = label;
	}
	
	public GALevel Level
	{
		get { return _level; }
		set { _level = value; }
	}
	
	public string Category
	{
		get{ return _category; }
		set{ _category = value; }
	}
	
	public string Variable
	{
		get{ return _variable; }
		set{ _variable = value; }
	}
	
	public string Label
	{
		get{ return _label; }
		set{ _label = value; }
	}
	
	public void Start(){
		_startTime = GoogleAnalytics.instance.GetEpochTime();
	}
	
	public void Stop()
	{
		_stopTime  = GoogleAnalytics.instance.GetEpochTime();
	}
	
	private long ElapsedTime()
	{
		if (_startTime == 0 || _stopTime == 0)
		{
			throw new System.ArgumentException("To get the elapsed time please specify the start and end time");	
		}
		return (_stopTime - _startTime) * 1000;
	}
	
	public string ToUrlParamString()
	{
		string utme = "14(90!";
		utme +=  System.Uri.EscapeDataString( Variable );
		utme += "*" + System.Uri.EscapeDataString( Category );
		utme += "*" + ElapsedTime().ToString();
		if (Label != null)
		{
			utme += "*" + System.Uri.EscapeDataString( Label );
		}
		utme += ")";
		utme += "(90!" + ElapsedTime().ToString() + ")";
		
		return utme;
	}
}


public class GAPurchaseItem
{
	private string _sku;
	private string _productName;
	private string _category; // or it could be a variation such as "Red", 
	private decimal _price;
	private int _quantity;
	
	public GAPurchaseItem(string sku, string productName, decimal price) 
	{
		//_transactionId = transactionId;
		_sku           = sku;
		_productName   = productName;
		_price         = price;
		_quantity      = 1;
	}
	
	public GAPurchaseItem(string sku, string productName, decimal price, int quantity) 
	{
		//_transactionId = transactionId;
		_sku           = sku;
		_productName   = productName;
		_price         = price;
		_quantity      = quantity;
	}
	
	public GAPurchaseItem(string sku, string productName, string category, decimal price)
	{
		_sku         = sku;
		_productName = productName;
		_category    = category;
		_price       = price;
		_quantity    = 1;
	}
	
	public GAPurchaseItem(string sku, string productName, string category, decimal price, int quantity)
	{
		_sku         = sku;
		_productName = productName;
		_category    = category;
		_price       = price;
		_quantity    = quantity;
	}
	
	public Hashtable ToParamHashtable()
	{
		//System.Uri.EscapeDataString( Category );
		Hashtable param = new Hashtable();
		param["utmtid"]  = System.Uri.EscapeDataString("(not set)");
		param["utmipc"] = System.Uri.EscapeDataString(_sku);
		param["utmipn"] = System.Uri.EscapeDataString(_productName);
		param["utmipr"] = _price.ToString();
		param["utmiqt"] = _quantity.ToString();
		
		if (!string.IsNullOrEmpty(_category)) {
			param["utmiva"] = System.Uri.EscapeDataString(_category);
		}
		
		return param;
	}
}