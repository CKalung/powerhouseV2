using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using System.Runtime.CompilerServices;


namespace DMPStaticClientsSession
{
    public static class ClientsSessionCollections
    {
        static Hashtable clients = new Hashtable();
        struct sessionStruct
        {
            public string clientInfo;
            public DateTime lastAccessTime;
            public string clientStatus;
            public string clientNetAddress;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void addClient(string userId, string userStatus, string userNetAddress, string userInfo)
        {

            clients.Add(userId, new sessionStruct { 
                clientNetAddress = userNetAddress, clientInfo = userInfo, 
                clientStatus = userStatus, lastAccessTime = DateTime.Now
                                                    });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool updateClient(string userId, string userStatus, string userInfo)
        {
            if (!clients.ContainsKey(userId)) return false;
            sessionStruct isinya = (sessionStruct)clients[userId];
            isinya.clientStatus = userStatus; isinya.clientInfo = userInfo;
            clients[userId] = isinya;
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool updateClientStatus(string userId, string userStatus)
        {
            if (!clients.ContainsKey(userId)) return false;
            sessionStruct isinya = (sessionStruct)clients[userId];
            isinya.clientStatus = userStatus; 
            clients[userId] = isinya;
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool updateClientInfo(string userId, string userInfo)
        {
            if (!clients.ContainsKey(userId)) return false;
            sessionStruct isinya = (sessionStruct)clients[userId];
            isinya.clientInfo = userInfo;
            clients[userId] = isinya;
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool updateClientAccessTime(string userId)
        {
            if (!clients.ContainsKey(userId)) return false;
            sessionStruct isinya = (sessionStruct)clients[userId];
            isinya.lastAccessTime = DateTime.Now;
            clients[userId] = isinya;
            return true;
        }

    }
}
