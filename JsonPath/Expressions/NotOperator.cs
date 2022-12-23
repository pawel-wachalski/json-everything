﻿namespace Json.Path.Expressions;

internal class NotOperator : IUnaryLogicalOperator
{
	public int Precedence => 20;

	public bool Evaluate(bool value)
	{
		return !value;
	}
}