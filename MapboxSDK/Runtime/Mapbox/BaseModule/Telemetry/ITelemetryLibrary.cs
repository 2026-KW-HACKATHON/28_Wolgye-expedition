namespace Mapbox.BaseModule.Telemetry
{
	public interface ITelemetryLibrary
	{
		void Initialize(string accessToken);
		void SendTurnstile();
		void SetLocationCollectionState(bool enable);
		void SendSdkEvent();
	}
}