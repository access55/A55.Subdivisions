namespace A55.Subdivisions.Aws;

record BaseArn(string Value)
{
    public override string ToString() => Value;
}
