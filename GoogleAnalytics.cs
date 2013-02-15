using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoogleAnalytics : MonoBehaviour {
	
	public string propertyID;
	public string defaultURL;
	
	public static GoogleAnalytics instance;
	
	private Hashtable sessionRequestParams = new Hashtable();
	private List<Hashtable> eventList= new List<Hashtable>();
	
	private string currentSessionStartTime;
	private string lastSessionStartTime;
	private string firstSessionStartTime;
	private int sessions;
	
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
		
		levelRequestParams["utmdt"] = System.Uri.EscapeDataString( Application.loadedLevelName );
		
		// Will be overridden if you use GALevel
		levelRequestParams["utmp"]  = System.Uri.EscapeDataString( Application.loadedLevelName );
		
		return levelRequestParams;
	}
	
	public void Add(GALevel gaLevel)
	{
		Hashtable eventSpecificParams = (Hashtable)LevelSpecificRequestParams().Clone();
		
		eventSpecificParams["utmcc"] = CookieData();
		eventSpecificParams["utmn"]  = Random.Range(1000000000,2000000000).ToString();
		eventSpecificParams["utmp"]  = gaLevel.ToUrlParamString();
		
		eventList.Add(eventSpecificParams);
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
		eventList.Add(eventSpecificParams);
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
		
		eventList.Add(eventSpecificParams);
	}
	
	//
	// turns all the events into request urls
	//
	public void Dispatch()
	{
		for (int evtIndex=0; evtIndex<eventList.Count; evtIndex++)
		{
			string urlRequestParams = BuildRequestString(eventList[evtIndex]);
			string url = "http://www.google-analytics.com/__utm.gif?" + urlRequestParams;
			StartCoroutine( MakeRequest(url, eventList[evtIndex])  );
		}
	}
	
	
	//
	//  send the request to google
	//
	IEnumerator MakeRequest(string url, Hashtable evt)
	{
		WWW request = new WWW(url);
		
		yield return request;
		
		if(request.error == null)
		{
			if (request.responseHeaders.ContainsKey("STATUS"))
			{
				if (request.responseHeaders["STATUS"] == "HTTP/1.1 200 OK")	
				{
					eventList.Remove(evt);	
				}else{
					Debug.LogWarning(request.responseHeaders["STATUS"]);	
				}
			}else{
				Debug.LogWarning("Event failed to send to Google");	
			}
		}else{
			Debug.LogWarning(request.error.ToString());	
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
			utme += "*" + Value;
		}
		
		utme += ")";
			
		return utme;	
	}
}

public class GAUserTimer
{
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