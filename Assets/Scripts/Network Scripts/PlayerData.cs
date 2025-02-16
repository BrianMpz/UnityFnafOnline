using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public PlayerRoles role;
    public FixedString128Bytes playerName;
    public FixedString128Bytes vivoxId;

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId && role == other.role && playerName == other.playerName.ToString() && vivoxId == other.vivoxId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref role);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref vivoxId);
    }
}
