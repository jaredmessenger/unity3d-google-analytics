using UnityEngine;
using System.Collections;

public class GoogleAnalytics : MonoBehaviour {
	
	public string PropertyID;
	public string DefaultURL;
	
	public static GoogleAnalytics instance;
	
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
		//Get the player prefs last time played and current time

    }
	
	public void SetCustomVar(int index, string name, string value, int scope)
	{
		// optional scope values: 1 (visitor-level), 2 (session-level), or 3 (page-level).
		// https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingCustomVariables	
		Debug.Log("Custom Var");
	}
	
	public void TrackLevel()
	{
		Debug.Log(Application.loadedLevelName);	
	}
	
	public void TrackEvent(string category, string label, string action, int value)
	{
		Debug.Log(category);	
	}
	
	public void TrackTiming()
	{
	 	// https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingTiming
		Debug.Log("Timer");
	}
	
	public void Dispatch()
	{
		// Send the data to the Google Servers
		Debug.Log("Sending data to Google");
	}
	
	private long PlayerID()
	{
        return Hash (SystemInfo.deviceUniqueIdentifier );
	}
	
	private void SetCookieData()
	{
		
	}
	
	private long Hash(string url)
	{
		if(url.Length < 3) return Random.Range(10000000,99999999);
		
		int hash = 0;
		int hashCmp = 0;
		for(int urlLen=url.Length-1; urlLen>=0; urlLen--){
			int charCode = (int)url[urlLen];
            hash    = (hash<<6&268435455) + charCode + (charCode<<14);
            hashCmp = hash&266338304;
            hash    = hashCmp != 0 ? hash^hashCmp>>21 : hash;
		}
		return hash;
	}
	
	private long GetEpochTime() 
	{
		System.DateTime currentTime = System.DateTime.Now;
		System.DateTime epochStart  = System.Convert.ToDateTime("1/1/1970 0:00:00 AM");
		System.TimeSpan timeSpan    = currentTime.Subtract(epochStart);
		
		long epochTime = ((((((timeSpan.Days * 24) + timeSpan.Hours) * 60) + timeSpan.Minutes) * 60) + timeSpan.Seconds);
		
		return epochTime;
	}
}