using System;

namespace SharedClasses
{
	public static class InstantMessengerShared
	{
		// Packet types
		public const int IM_Hello = 2012;      // Hello
		public const byte IM_OK = 0;           // OK
		public const byte IM_Login = 1;        // Login
		public const byte IM_Register = 2;     // Register
		public const byte IM_TooUsername = 3;  // Too long username
		public const byte IM_TooPassword = 4;  // Too long password
		public const byte IM_Exists = 5;       // Already exists
		public const byte IM_NoExists = 6;     // Doesn't exist
		public const byte IM_WrongPass = 7;    // Wrong password
		public const byte IM_IsAvailable = 8;  // Is user available?
		public const byte IM_Send = 9;         // Send message
		public const byte IM_Received = 10;    // Message received
		//Added by Francois
		public const string IM_ServerUsername = "[SERVER]";
		public const byte IM_AskServer = 11;
		public const byte IM_GetLoggedInUsers = 12;
	}
}