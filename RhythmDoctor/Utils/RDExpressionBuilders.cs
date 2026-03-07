using System.Text;

namespace RhythmBase.RhythmDoctor.Components
{
	public interface IRDExpression
	{
		string Serialize();
	}
	internal interface IOp
	{
		byte Priority { get; }
		bool IsStatic { get; }
		string Serialize();
	}
	internal interface IEndpointOp : IOp { }
	internal interface IConstantOp : IEndpointOp { }
	internal interface IVariableOp : IEndpointOp { }
	internal interface IUnaryOp : IOp { }
	internal interface IBinaryOp : IOp { }
	internal interface IFunctionOp : IOp
	{
		string Name { get; }
		IOp[] Args { get; }
	}
	internal interface IBooleanOp : IOp
	{
		IBooleanOp Simplified();
	}
	internal interface INumericOp : IOp
	{
		INumericOp Simplified();
	}
	internal readonly record struct BooleanValue(bool Value) : IConstantOp, IBooleanOp
	{
		public byte Priority => 0;
		public bool IsStatic => true;
		public IBooleanOp Simplified() => this;
		public override string ToString() => Value.ToString();
		public string Serialize() => Value ? "true" : "false";
	}
	internal readonly record struct NumericValue(float Value) : IConstantOp, INumericOp
	{
		public byte Priority => 0;
		public bool IsStatic => true;
		public INumericOp Simplified() => this;
		public override string ToString() => Value.ToString();
		public string Serialize() => Value.ToString("R");
	}
	internal readonly record struct StringValue(string Value) : IConstantOp
	{
		public byte Priority => 0;
		public bool IsStatic => true;
		public override string ToString() => $"\"{Value}\"";
		public string Serialize() => $"str:{Value}";
	}
	internal readonly record struct BooleanVariable(int Index) : IVariableOp, IBooleanOp
	{
		public byte Priority => 0;
		public bool IsStatic => false;
		public IBooleanOp Simplified() => this;
		public override string ToString() => $"b{Index}";
		public string Serialize() => $"b{Index}";
	}
	internal readonly record struct FloatVariable(int Index) : IVariableOp, INumericOp
	{
		public byte Priority => 0;
		public bool IsStatic => false;
		public INumericOp Simplified() => this;
		public override string ToString() => $"f{Index}";
		public string Serialize() => $"f{Index}";
	}
	internal readonly record struct IntVariable(int Index) : IVariableOp, INumericOp
	{
		public byte Priority => 0;
		public bool IsStatic => false;
		public INumericOp Simplified() => this;
		public override string ToString() => $"i{Index}";
		public string Serialize() => $"i{Index}";
	}
	internal readonly record struct NamedBooleanVariable(string Name) : IVariableOp, IBooleanOp
	{
		public byte Priority => 0;
		public bool IsStatic => false;
		public IBooleanOp Simplified() => this;
		public override string ToString() => Name;
		public string Serialize() => Name;
	}
	internal readonly record struct NamedNumericVariable(string Name) : IVariableOp, INumericOp
	{
		public byte Priority => 0;
		public bool IsStatic => false;
		public INumericOp Simplified() => this;
		public override string ToString() => Name;
		public string Serialize() => Name;
	}
	internal readonly record struct UnaryBooleanOp(IBooleanOp Value, UnaryOperator Op) : IUnaryOp, IBooleanOp
	{
		public byte Priority => (byte)((byte)Op >> 2);
		public bool IsStatic => Value.IsStatic;
		public IBooleanOp Simplified()
		{
			IBooleanOp innerValue = Value.Simplified();
			IBooleanOp value = Op switch
			{
				UnaryOperator.Not => innerValue switch
				{
					BooleanValue boolVal => new BooleanValue(!boolVal.Value),
					_ => this,
				},
				_ => throw new InvalidOperationException(),
			};
			return value;

		}
		public string Serialize()
		{
			var innerValue = Simplified();
			if (innerValue is not UnaryBooleanOp tmp || tmp != this)
			{
				return innerValue.Serialize();
			}
			bool needParens = Value.Priority > 0 && Value.Priority < Priority;
			string opStr = needParens ? $"({Value.Serialize()})" : Value.Serialize();
			return Op switch
			{
				UnaryOperator.Not => $"Not {opStr}",
				_ => throw new InvalidOperationException(),
			};
		}
	}
	internal readonly record struct UnaryNumericOp(INumericOp Value, UnaryOperator Op) : IUnaryOp, INumericOp
	{
		public byte Priority => (byte)((byte)Op >> 2);
		public bool IsStatic => Value.IsStatic;
		public INumericOp Simplified()
		{
			INumericOp innerValue = Value.Simplified();
			INumericOp value = Op switch
			{
				UnaryOperator.Negate => innerValue switch
				{
					NumericValue floatVal => new NumericValue(-floatVal.Value),
					_ => this,
				},
				UnaryOperator.Positive => innerValue,
				_ => throw new InvalidOperationException(),
			};
			return value;
		}
		public string Serialize()
		{
			INumericOp innerValue = Simplified();
			if (innerValue is not UnaryNumericOp tmp || tmp != this)
			{
				return innerValue.Serialize();
			}
			bool needParens = Value.Priority > 0 && Value.Priority < Priority;
			string opStr = needParens ? $"({Value.Serialize()})" : Value.Serialize();
			return Op switch
			{
				UnaryOperator.Negate => $"-{opStr}",
				UnaryOperator.Positive => $"+{opStr}",
				_ => throw new InvalidOperationException(),
			};
		}
	}
	internal readonly record struct BinaryBooleanOp : IBinaryOp, IBooleanOp
	{
		public IOp Left { get; }
		public IOp Right { get; }
		public BinaryOperator Op { get; }
		public byte Priority => (byte)((byte)Op >> 2);
		public bool IsStatic => Left.IsStatic && Right.IsStatic;
		public BinaryBooleanOp(IBooleanOp left, IBooleanOp right, BinaryOperator op)
		{
			Left = left;
			Right = right;
			Op = op;
		}
		public BinaryBooleanOp(INumericOp left, INumericOp right, BinaryOperator op)
		{
			Left = left;
			Right = right;
			Op = op;
		}
		public IBooleanOp Simplified()
		{
			switch (Left, Right)
			{
				case (IBooleanOp leftBool, IBooleanOp rightBool):
					{
						IBooleanOp leftValue = leftBool.Simplified();
						IBooleanOp rightValue = rightBool.Simplified();
						IBooleanOp value = Op switch
						{
							BinaryOperator.Or => (leftValue, rightValue) switch
							{
								(BooleanValue leftVal, BooleanValue rightVal) => new BooleanValue(leftVal.Value || rightVal.Value),
								(BooleanValue leftVal, _) when leftVal.Value => new BooleanValue(true),
								(BooleanValue leftVal, _) when !leftVal.Value => rightValue,
								(_, BooleanValue rightVal) when rightVal.Value => new BooleanValue(true),
								(_, BooleanValue rightVal) when !rightVal.Value => leftValue,
								_ => this,
							},
							BinaryOperator.And => (leftValue, rightValue) switch
							{
								(BooleanValue leftVal, BooleanValue rightVal) => new BooleanValue(leftVal.Value && rightVal.Value),
								(BooleanValue leftVal, _) when leftVal.Value => rightValue,
								(BooleanValue leftVal, _) when !leftVal.Value => new BooleanValue(false),
								(_, BooleanValue rightVal) when rightVal.Value => leftValue,
								(_, BooleanValue rightVal) when !rightVal.Value => new BooleanValue(false),
								_ => this,
							},
							BinaryOperator.Equal => (leftValue, rightValue) switch
							{
								(BooleanValue leftVal, BooleanValue rightVal) => new BooleanValue(leftVal.Value == rightVal.Value),
								(BooleanValue leftVal, _) when leftVal.Value => rightValue,
								(BooleanValue leftVal, _) when !leftVal.Value => new UnaryBooleanOp(rightValue, UnaryOperator.Not),
								(_, BooleanValue rightVal) when rightVal.Value => leftValue,
								(_, BooleanValue rightVal) when !rightVal.Value => new UnaryBooleanOp(leftValue, UnaryOperator.Not),
								_ => this,
							},
							BinaryOperator.NotEqual => (leftValue, rightValue) switch
							{
								(BooleanValue leftVal, BooleanValue rightVal) => new BooleanValue(leftVal.Value == rightVal.Value),
								(BooleanValue leftVal, _) when leftVal.Value => new UnaryBooleanOp(rightValue, UnaryOperator.Not),
								(BooleanValue leftVal, _) when !leftVal.Value => rightValue,
								(_, BooleanValue rightVal) when rightVal.Value => new UnaryBooleanOp(leftValue, UnaryOperator.Not),
								(_, BooleanValue rightVal) when !rightVal.Value => leftValue,
								_ => this,
							},
							_ => throw new InvalidOperationException(),
						};
						return value;
					}
				case (INumericOp leftNum, INumericOp rightNum):
					{
						INumericOp leftValue = leftNum.Simplified();
						INumericOp rightValue = rightNum.Simplified();
						if (!leftValue.IsStatic || !rightValue.IsStatic)
							return this;
						float leftFloat = leftValue switch
						{
							NumericValue floatVal => floatVal.Value,
							_ => throw new InvalidOperationException(),
						};
						float rightFloat = rightValue switch
						{
							NumericValue floatVal => floatVal.Value,
							_ => throw new InvalidOperationException(),
						};
						IBooleanOp value = new BooleanValue(Op switch
						{
							BinaryOperator.Equal => leftFloat == rightFloat,
							BinaryOperator.NotEqual => leftFloat != rightFloat,
							BinaryOperator.GreaterThan => leftFloat > rightFloat,
							BinaryOperator.GreaterThanOrEqual => leftFloat >= rightFloat,
							BinaryOperator.LessThan => leftFloat < rightFloat,
							BinaryOperator.LessThanOrEqual => leftFloat <= rightFloat,
							_ => throw new InvalidOperationException(),
						});
						return value;
					}
				default:
					throw new InvalidOperationException();
			}
		}
		public string Serialize()
		{
			IBooleanOp value = Simplified();
			if (value is not BinaryBooleanOp tmp || tmp != this)
			{
				return value.Serialize();
			}
			bool needParensLeft = Left.Priority > 0 && Left.Priority < Priority;
			bool needParensRight = Right.Priority > 0 && Right.Priority < Priority;
			string leftStr = needParensLeft ? $"({Left.Serialize()})" : Left.Serialize();
			string rightStr = needParensRight ? $"({Right.Serialize()})" : Right.Serialize();
			return Op switch
			{
				BinaryOperator.Or => $"{leftStr} Or {rightStr}",
				BinaryOperator.And => $"{leftStr} And {rightStr}",
				BinaryOperator.Equal => $"{leftStr} == {rightStr}",
				BinaryOperator.NotEqual => $"{leftStr} <> {rightStr}",
				BinaryOperator.GreaterThan => $"{leftStr} > {rightStr}",
				BinaryOperator.GreaterThanOrEqual => $"{leftStr} >= {rightStr}",
				BinaryOperator.LessThan => $"{leftStr} < {rightStr}",
				BinaryOperator.LessThanOrEqual => $"{leftStr} <= {rightStr}",
				_ => throw new InvalidOperationException(),
			};
		}
	}
	internal readonly record struct BinaryNumericOp(INumericOp Left, INumericOp Right, BinaryOperator Op) : IBinaryOp, INumericOp
	{
		public byte Priority => (byte)((byte)Op >> 2);
		public bool IsStatic => Left.IsStatic && Right.IsStatic;
		public INumericOp Simplified()
		{
			INumericOp leftValue = Left.Simplified();
			INumericOp rightValue = Right.Simplified();
			switch (leftValue, rightValue)
			{
				case (_, _) when leftValue.IsStatic && rightValue.IsStatic:
					float leftFloat = leftValue switch
					{
						NumericValue floatVal => floatVal.Value,
						_ => throw new InvalidOperationException(),
					};
					float rightFloat = rightValue switch
					{
						NumericValue floatVal => floatVal.Value,
						_ => throw new InvalidOperationException(),
					};
					INumericOp value = new NumericValue(Op switch
					{
						BinaryOperator.Add => leftFloat + rightFloat,
						BinaryOperator.Subtract => leftFloat - rightFloat,
						BinaryOperator.Multiply => leftFloat * rightFloat,
						BinaryOperator.Divide => leftFloat / rightFloat,
						BinaryOperator.Modulo => leftFloat % rightFloat,
						_ => throw new InvalidOperationException(),
					});
					return value;
				case (NumericValue leftVal1, _) when
					(leftVal1.Value == 0 && Op is BinaryOperator.Add or BinaryOperator.Divide) ||
					(leftVal1.Value == 1 && Op is BinaryOperator.Multiply or BinaryOperator.Subtract):
					return rightValue;
				case (_, NumericValue rightVal1) when
					(rightVal1.Value == 0 && Op is BinaryOperator.Add or BinaryOperator.Divide) ||
					(rightVal1.Value == 1 && Op is BinaryOperator.Multiply or BinaryOperator.Subtract):
					return leftValue;
				case (NumericValue leftVal1, _) when
					(leftVal1.Value == 0 && Op is BinaryOperator.Multiply or BinaryOperator.Divide):
					return new NumericValue(0);
				case (_, NumericValue rightVal1) when
					(rightVal1.Value == 0 && Op is BinaryOperator.Multiply) ||
					(rightVal1.Value == 1 && Op is BinaryOperator.Modulo):
					return new NumericValue(0);
				default:
					return new BinaryNumericOp(leftValue, rightValue, Op);

			}
		}
		public string Serialize()
		{
			INumericOp value = Simplified();
			if (value is not BinaryNumericOp tmp || tmp != this)
			{
				return value.Serialize();
			}
			bool needParensLeft = Left.Priority > 0 && Left.Priority < Priority;
			bool needParensRight =
				Right.Priority > 0 && Right.Priority < Priority ||
				(Op is BinaryOperator.Divide && Right is BinaryNumericOp right && right.Op is not BinaryOperator.Divide) ||
				(Op is BinaryOperator.Modulo && Right is BinaryNumericOp);
			string leftStr = needParensLeft ? $"({Left.Serialize()})" : Left.Serialize();
			string rightStr = needParensRight ? $"({Right.Serialize()})" : Right.Serialize();
			return Op switch
			{
				BinaryOperator.Add => $"{leftStr} + {rightStr}",
				BinaryOperator.Subtract => $"{leftStr} - {rightStr}",
				BinaryOperator.Multiply => $"{leftStr} * {rightStr}",
				BinaryOperator.Divide => $"{leftStr} / {rightStr}",
				BinaryOperator.Modulo => $"{leftStr} % {rightStr}",
				_ => throw new InvalidOperationException(),
			};
		}
	}
	internal readonly record struct FunctionVoidOp(string Name, IOp[] Args, bool IsSelfStatic) : IFunctionOp, IOp
	{
		public byte Priority => 0;
		public bool IsStatic => IsSelfStatic && Args.All(arg => arg.IsStatic);
		public string Serialize()
		{
			string argsStr = string.Join(", ", Args.Select(arg => arg.Serialize()));
			return $"{Name}({argsStr})";
		}
	}
	internal readonly record struct FunctionBooleanOp(string Name, IOp[] Args, bool IsSelfStatic) : IFunctionOp, IBooleanOp
	{
		public byte Priority => 0;
		public bool IsStatic => IsSelfStatic && Args.All(arg => arg.IsStatic);
		public IBooleanOp Simplified() => this;
		public string Serialize()
		{
			string argsStr = string.Join(", ", Args.Select(arg => arg.Serialize()));
			return $"{Name}({argsStr})";
		}
	}
	internal readonly record struct FunctionNumericOp(string Name, IOp[] Args, bool IsSelfStatic) : IFunctionOp, INumericOp
	{
		public byte Priority => 0;
		public bool IsStatic => IsSelfStatic && Args.All(arg => arg.IsStatic);
		public INumericOp Simplified() => this;
		public string Serialize()
		{
			string argsStr = string.Join(", ", Args.Select(arg => arg.Serialize()));
			return $"{Name}({argsStr})";
		}
	}
	/*

	0  value
	1  = += -= *= /= %=
	2  ??
	3  ||
	4  &&
	5  |
	6  ^
	7  &
	8  == !=
	9  < > <= >=
	10 << >>
	11 + - (binary)
	12 * / %
	13 + - ! ~ (unary)
	14 ++ -- . -> [] ()

	 */
	internal enum BinaryOperator : byte
	{
		Or = (3 << 2) | 0,
		And = (4 << 2) | 0,
		Equal = (8 << 2) | 0,
		NotEqual = (8 << 2) | 1,
		GreaterThan = (9 << 2) | 0,
		GreaterThanOrEqual = (9 << 2) | 1,
		LessThan = (9 << 2) | 2,
		LessThanOrEqual = (9 << 2) | 3,
		Add = (11 << 2) | 0,
		Subtract = (11 << 2) | 1,
		Multiply = (12 << 2) | 0,
		Divide = (12 << 2) | 1,
		Modulo = (12 << 2) | 2,
	}
	internal enum UnaryOperator : byte
	{
		Not = (13 << 2) | 0,
		Negate = (13 << 2) | 1,
		Positive = (13 << 2) | 2,
	}
	public struct RDBooleanExpression : IRDExpression
	{
		public class RDBooleanExpressionBuilder()
		{
			public RDBooleanExpression this[int index] => new(new BooleanVariable(index));
		}
		internal IBooleanOp _v;
		internal RDBooleanExpression(IBooleanOp v) => _v = v;
		public static implicit operator RDBooleanExpression(bool v) => new(new BooleanValue(v));
		public static RDBooleanExpression operator !(RDBooleanExpression v) => new(new UnaryBooleanOp(v._v, UnaryOperator.Not));
		public static RDBooleanExpression operator |(RDBooleanExpression left, RDBooleanExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.Or));
		public static RDBooleanExpression operator &(RDBooleanExpression left, RDBooleanExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.And));
		public static RDBooleanExpression operator ==(RDBooleanExpression left, RDBooleanExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.Equal));
		public static RDBooleanExpression operator !=(RDBooleanExpression left, RDBooleanExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.NotEqual));
		public readonly string Serialize() => _v.Serialize();
	}
	public struct RDNumericExpression : IRDExpression
	{
		public class RDIntegerExpressionBuilder()
		{
			public RDNumericExpression this[int index] => new(new IntVariable(index));
		}
		public class RDFloatExpressionBuilder()
		{
			public RDNumericExpression this[int index] => new(new FloatVariable(index));
		}
		internal INumericOp _v;
		internal RDNumericExpression(INumericOp v) => _v = v;
		public static implicit operator RDNumericExpression(float v) => new(new NumericValue(v));
		public static implicit operator RDNumericExpression(int v) => new(new NumericValue(v));
		public static RDNumericExpression operator +(RDNumericExpression v) => new(new UnaryNumericOp(v._v, UnaryOperator.Positive));
		public static RDNumericExpression operator -(RDNumericExpression v) => new(new UnaryNumericOp(v._v, UnaryOperator.Negate));
		public static RDNumericExpression operator +(RDNumericExpression left, RDNumericExpression right) => new(new BinaryNumericOp(left._v, right._v, BinaryOperator.Add));
		public static RDNumericExpression operator -(RDNumericExpression left, RDNumericExpression right) => new(new BinaryNumericOp(left._v, right._v, BinaryOperator.Subtract));
		public static RDNumericExpression operator *(RDNumericExpression left, RDNumericExpression right) => new(new BinaryNumericOp(left._v, right._v, BinaryOperator.Multiply));
		public static RDNumericExpression operator /(RDNumericExpression left, RDNumericExpression right) => new(new BinaryNumericOp(left._v, right._v, BinaryOperator.Divide));
		public static RDNumericExpression operator %(RDNumericExpression left, RDNumericExpression right) => new(new BinaryNumericOp(left._v, right._v, BinaryOperator.Modulo));
		public static RDBooleanExpression operator ==(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.Equal));
		public static RDBooleanExpression operator !=(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.NotEqual));
		public static RDBooleanExpression operator >(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.GreaterThan));
		public static RDBooleanExpression operator >=(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.GreaterThanOrEqual));
		public static RDBooleanExpression operator <(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.LessThan));
		public static RDBooleanExpression operator <=(RDNumericExpression left, RDNumericExpression right) => new(new BinaryBooleanOp(left._v, right._v, BinaryOperator.LessThanOrEqual));
		public readonly string Serialize() => _v.Serialize();
	}
	public struct RDStringExpression : IRDExpression
	{
		internal StringValue _v;
		internal RDStringExpression(StringValue v) => _v = v;
		public static implicit operator RDStringExpression(string v) => new(new StringValue(v));
		public static implicit operator RDStringExpression(Enum v) => new(new StringValue(v.ToString()));
		public readonly string Serialize() => _v.Serialize();
	}
	public struct RDVoidExpression : IRDExpression
	{
		internal IOp _v;
		internal RDVoidExpression(FunctionVoidOp op) => _v = op;
		public readonly string Serialize() => _v.Serialize();
	}
	public static class RDExpressionBuilder
	{
#pragma warning disable IDE1006
		public static RDNumericExpression.RDFloatExpressionBuilder f => new();
		public static RDNumericExpression.RDIntegerExpressionBuilder i => new();
		public static RDBooleanExpression.RDBooleanExpressionBuilder b => new();
		public static RDNumericExpression f0 => new(new FloatVariable(0));
		public static RDNumericExpression f1 => new(new FloatVariable(1));
		public static RDNumericExpression f2 => new(new FloatVariable(2));
		public static RDNumericExpression f3 => new(new FloatVariable(3));
		public static RDNumericExpression f4 => new(new FloatVariable(4));
		public static RDNumericExpression f5 => new(new FloatVariable(5));
		public static RDNumericExpression f6 => new(new FloatVariable(6));
		public static RDNumericExpression f7 => new(new FloatVariable(7));
		public static RDNumericExpression f8 => new(new FloatVariable(8));
		public static RDNumericExpression f9 => new(new FloatVariable(9));
		public static RDNumericExpression i0 => new(new IntVariable(0));
		public static RDNumericExpression i1 => new(new IntVariable(1));
		public static RDNumericExpression i2 => new(new IntVariable(2));
		public static RDNumericExpression i3 => new(new IntVariable(3));
		public static RDNumericExpression i4 => new(new IntVariable(4));
		public static RDNumericExpression i5 => new(new IntVariable(5));
		public static RDNumericExpression i6 => new(new IntVariable(6));
		public static RDNumericExpression i7 => new(new IntVariable(7));
		public static RDNumericExpression i8 => new(new IntVariable(8));
		public static RDNumericExpression i9 => new(new IntVariable(9));
		public static RDBooleanExpression b0 => new(new BooleanVariable(0));
		public static RDBooleanExpression b1 => new(new BooleanVariable(1));
		public static RDBooleanExpression b2 => new(new BooleanVariable(2));
		public static RDBooleanExpression b3 => new(new BooleanVariable(3));
		public static RDBooleanExpression b4 => new(new BooleanVariable(4));
		public static RDBooleanExpression b5 => new(new BooleanVariable(5));
		public static RDBooleanExpression b6 => new(new BooleanVariable(6));
		public static RDBooleanExpression b7 => new(new BooleanVariable(7));
		public static RDBooleanExpression b8 => new(new BooleanVariable(8));
		public static RDBooleanExpression b9 => new(new BooleanVariable(9));
		public static RDNumericExpression bpm => new(new NamedNumericVariable("bpm"));
#pragma warning restore IDE1006
		public static RDVoidExpression CallVoid(string name, params IRDExpression[] args) => new(new FunctionVoidOp(name, [.. args.Select(arg => arg switch {
			RDBooleanExpression exp => exp._v as IOp,
			RDNumericExpression exp => exp._v as IOp,
			RDStringExpression exp => exp._v as IOp,
			_=> throw new InvalidOperationException($"Unsupported expression type: {arg.GetType()} with value {arg}")
		})], false));
		public static RDBooleanExpression CallBool(string name, params IRDExpression[] args) => new(new FunctionBooleanOp(name, [.. args.Select(arg => arg switch {
			RDBooleanExpression exp => exp._v as IOp,
			RDNumericExpression exp => exp._v as IOp,
			RDStringExpression exp => exp._v as IOp,
			_=> throw new InvalidOperationException($"Unsupported expression type: {arg.GetType()} with value {arg}")
		})], false));
		public static RDNumericExpression CallNumeric(string name, params IRDExpression[] args) => new(new FunctionNumericOp(name, [.. args.Select(arg => arg switch {
			RDBooleanExpression exp => exp._v as IOp,
			RDNumericExpression exp => exp._v as IOp,
			RDStringExpression exp => exp._v as IOp,
			_=> throw new InvalidOperationException($"Unsupported expression type: {arg.GetType()} with value {arg}")
		})], false));
		public static RDNumericExpression IIf(RDBooleanExpression condition, RDNumericExpression trueExpr, RDNumericExpression falseExpr) => CallNumeric("IIf", condition, trueExpr, falseExpr);
		//public static RDExpression Assignment()
		public static RDNumericExpression Rand(RDNumericExpression max) => CallNumeric("Rand", max);
#if DEBUG
		public static string PrintTree(IRDExpression expression)
		{
			static string ToString(IOp op)
			{
				static string CombineLines(params string[][] lines)
				{
					if (lines.Length == 0) return "";
					if (lines.Length == 1) return "│\n" + string.Join("\n", lines[0]);
					string header = "";
					string[] results = new string[lines.Max(i => i.Length)];
					for (int i = 0; i < lines.Length - 1; ++i)
					{
						int maxLen = lines[i].Max(x => x.Length);
						if (i == 0)
							header += "├";
						else
							header += "┬";
						header += new string('─', maxLen);
						for (int j = 0; j < lines[i].Length; ++j)
						{
							results[j] += lines[i][j] + new string(' ', maxLen - lines[i][j].Length + 1);
						}
						for (int j = lines[i].Length; j < results.Length; ++j)
						{
							results[j] += new string(' ', maxLen + 1);
						}
					}
					header += "┐";
					for (int i = 0; i < lines[lines.Length - 1].Length; ++i)
					{
						results[i] += lines[lines.Length - 1][i];
					}
					return header + "\n" + string.Join("\n", results);
				}
				if (op is IEndpointOp op1)
					return op1 switch
					{
						BooleanValue boolVal => boolVal.Value.ToString(),
						NumericValue floatVal => floatVal.Value.ToString(),
						StringValue strVal => strVal.Value.ToString(),
						BooleanVariable boolVar => $"b{boolVar.Index}",
						FloatVariable floatVar => $"f{floatVar.Index}",
						IntVariable intVar => $"i{intVar.Index}",
						NamedBooleanVariable namedBoolVar => namedBoolVar.ToString(),
						NamedNumericVariable namedNumVar => namedNumVar.ToString(),
						_ => throw new InvalidOperationException($"Unsupported IOp type: {op.GetType()} with value {op}")
					};
				else if (op is IUnaryOp op2)
				{
					string opStr = op2 switch
					{
						UnaryBooleanOp boolOp => boolOp.Op switch
						{
							UnaryOperator.Not => "Not",
							_ => throw new InvalidOperationException($"Unsupported UnaryOperator for IUnaryOp: {boolOp.Op}")
						},
						UnaryNumericOp numOp => numOp.Op switch
						{
							UnaryOperator.Negate => "-",
							UnaryOperator.Positive => "+",
							_ => throw new InvalidOperationException($"Unsupported UnaryOperator for IUnaryOp: {numOp.Op}")
						},
						_ => throw new InvalidOperationException($"Unsupported IUnaryOp type: {op.GetType()} with value {op}")
					};
					return $"""
						{opStr}
						│
						{op2 switch
					{
						UnaryBooleanOp boolOp => ToString(boolOp.Value),
						UnaryNumericOp numOp => ToString(numOp.Value),
						_ => throw new InvalidOperationException($"Unsupported IUnaryOp type: {op.GetType()} with value {op}")
					}}
						""";
				}
				else if (op is IBinaryOp op3)
				{
					string opStr = op3 switch
					{
						BinaryBooleanOp boolOp => boolOp.Op switch
						{
							BinaryOperator.Or => "Or",
							BinaryOperator.And => "And",
							BinaryOperator.Equal => "==",
							BinaryOperator.NotEqual => "<>",
							BinaryOperator.GreaterThan => ">",
							BinaryOperator.GreaterThanOrEqual => ">=",
							BinaryOperator.LessThan => "<",
							BinaryOperator.LessThanOrEqual => "<=",
							_ => throw new InvalidOperationException($"Unsupported BinaryOperator for IBinaryOp: {boolOp.Op}")
						},
						BinaryNumericOp numOp => numOp.Op switch
						{
							BinaryOperator.Add => "+",
							BinaryOperator.Subtract => "-",
							BinaryOperator.Multiply => "*",
							BinaryOperator.Divide => "/",
							BinaryOperator.Modulo => "%",
							_ => throw new InvalidOperationException($"Unsupported BinaryOperator for IBinaryOp: {numOp.Op}")
						},
						_ => throw new InvalidOperationException($"Unsupported IBinaryOp type: {op.GetType()} with value {op}")
					};
					string strL = op3 switch
					{
						BinaryBooleanOp boolOp => ToString(boolOp.Left),
						BinaryNumericOp numOp => ToString(numOp.Left),
						_ => throw new InvalidOperationException($"Unsupported IBinaryOp type: {op.GetType()} with value {op}")
					};
					string strR = op3 switch
					{
						BinaryBooleanOp boolOp => ToString(boolOp.Right),
						BinaryNumericOp numOp => ToString(numOp.Right),
						_ => throw new InvalidOperationException($"Unsupported IBinaryOp type: {op.GetType()} with value {op}")
					};
					string[] strLsp = strL.Split('\n');
					string[] strRsp = strR.Split('\n');
					return $"""
						{opStr}
						{CombineLines(strLsp, strRsp)}
						""";
				}
				else if (op is IFunctionOp funcOp)
				{
					return $"""
						{funcOp.Name}()
						{CombineLines([.. funcOp.Args.Select(arg => ToString(arg).Split('\n'))])}
						""";
				}
				else
				{
					throw new InvalidOperationException($"Unsupported IOp type: {op.GetType()} with value {op}");
				}
			}
			return ToString(expression switch
			{
				RDBooleanExpression boolExp => boolExp._v,
				RDNumericExpression numExp => numExp._v,
				RDStringExpression strExp => strExp._v,
				RDVoidExpression exp2 => exp2._v,
				_ => throw new InvalidOperationException($"Unsupported IRDExpression type: {expression.GetType()} with value {expression}")
			});
		}
#endif

	}
}