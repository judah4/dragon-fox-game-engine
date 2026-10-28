namespace DragonFox.Abstractions;

public readonly struct Vertex3D
{
    //Use fields for marshaling
    private readonly Vector3 _position;
    private readonly Vector3 _textureCoordinate;

    public Vector3 Position => _position;
    public Vector3 TextureCoordinate => _textureCoordinate;

    public Vertex3D(Vector3 position, Vector3 textureCoordinate)
    {
        _position = position;
        _textureCoordinate = textureCoordinate;
    }
}