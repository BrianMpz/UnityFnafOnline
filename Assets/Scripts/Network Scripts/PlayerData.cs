using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public PlayerRoles role;
    public FixedString128Bytes playerName;
    public FixedString128Bytes vivoxID;
    public uint experience;

    public readonly bool Equals(PlayerData other)
    {
        return clientId == other.clientId && role == other.role && playerName == other.playerName && vivoxID == other.vivoxID && experience == other.experience;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref role);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref vivoxID);
        serializer.SerializeValue(ref experience);
    }
}
