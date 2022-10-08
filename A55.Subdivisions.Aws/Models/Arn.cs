namespace A55.Subdivisions.Aws;

record BaseArn(string Value)
{
    public override string ToString() => Value;
}

record RuleArn(string Value) : BaseArn(Value);

record SnsArn(string Value) : BaseArn(Value);

record SqsArn(string Value) : BaseArn(Value);
