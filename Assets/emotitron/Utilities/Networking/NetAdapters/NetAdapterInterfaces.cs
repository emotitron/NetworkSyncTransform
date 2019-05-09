
namespace emotitron.Utilities.Networking
{

	public interface IOnNetSerialize { bool OnNetSerialize(int frameId, byte[] buffer, ref int bitposition); }
	public interface IOnNetDeserialize { void OnNetDeserialize(int sourceFrameId, int originFrameId, int localFrameId, byte[] buffer, ref int bitposition); }

	// Callbacks that the NST uses for notifications of network events
	public interface INetEvents
	{
		//void OnStartMaster();
		//void OnStartServer();
		//void OnStartClient();
		void OnStart();
		void OnStartLocalPlayer();
		void OnNetworkDestroy();
		void OnStartAuthority();
		void OnStopAuthority();
		void OnConnect(ServerClient svrclnt);
	}


	//public interface IOnMasterAwake
	//{
	//	void OnSettingsAwake();
	//}

	public enum ServerClient { Server, Client, Master }

	public interface IOnConnect
	{
		void OnConnect(ServerClient svrclnt);
	}

	public interface IOnStart
	{
		void OnStart();
	}

	public interface IOnStartMaster
	{
		void OnStartServer();
	}

	public interface IOnStartClient
	{
		void OnStartClient();
	}
	public interface IOnStartLocalPlayer
	{
		void OnStartLocalPlayer();
	}
	public interface IOnNetworkDestroy
	{
		void OnNetworkDestroy();
	}
	public interface IOnStartAuthority
	{
		void OnStartAuthority();
	}
	public interface IOnStopAuthority
	{
		void OnStopAuthority();
	}

	public interface IOnJoinRoom
	{
		void OnJoinRoom();
	}

	public interface IOnJoinRoomFailed
	{
		void OnJoinRoomFailed();
	}


}


