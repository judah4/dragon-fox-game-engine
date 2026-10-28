namespace DragonFox.Abstractions;

public readonly struct Vertex3D
{
    //Use fields for marshaling
    private readonly Vector3 _position;
    private readonly Vector2 _textureCoordinate;

    public Vector3 Position => _position;
    public Vector2 TextureCoordinate => _textureCoordinate;

    public Vertex3D(Vector3 position, Vector2 textureCoordinate)
    {
        _position = position;
        _textureCoordinate = textureCoordinate;
    }
}