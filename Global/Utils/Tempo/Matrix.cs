using System.Numerics;

namespace RhythmBase.Global.Utils.Tempo;

internal struct Matrix<T>(int rows, int columns) where T : struct, INumber<T>
{
	private readonly T[,] data = new T[rows, columns];
	public int Rows { get; private set; } = rows;
	public int Columns { get; private set; } = columns;
	public readonly T this[int row, int column]
	{
		get => data[row, column];
		set => data[row, column] = value;
	}
	public static Matrix<T> Identity(int size)
	{
		Matrix<T> matrix = new(size, size);
		for (int i = 0; i < size; i++)
		{
			matrix[i, i] = T.One;
		}
		return matrix;
	}
	public static Matrix<T> operator *(Matrix<T> a, Matrix<T> b)
	{
		if (a.Columns != b.Rows)
			throw new ArgumentException("Matrix dimensions are not compatible for multiplication.");
		Matrix<T> result = new(a.Rows, b.Columns);
		for (int i = 0; i < a.Rows; i++)
		{
			for (int j = 0; j < b.Columns; j++)
			{
				T sum = default;
				for (int k = 0; k < a.Columns; k++)
				{
					sum += a[i, k] * b[k, j];
				}
				result[i, j] = sum;
			}
		}
		return result;
	}
	public readonly Matrix<T> Transpose()
	{
		Matrix<T> result = new(Columns, Rows);
		for (int r = 0; r < Rows; ++r)
			for (int c = 0; c < Columns; ++c)
				result[c, r] += this[r, c];
		return result;
	}
}

internal class Givens
{
	private Matrix<double>
			m_oJ = new(2, 2),
			m_oQ = new(1, 1),
			m_oR = new(1, 1);
	public Matrix<double> Q => m_oQ;
	public Matrix<double> R => m_oR;
	private void GivensRotation(double a, double b)
	{
		double t, s, c;
		if (b == 0)
		{
			c = (a >= 0) ? 1 : -1;
			s = 0;
		}
		else if (a == 0)
		{
			c = 0;
			s = (b >= 0) ? -1 : 1;
		}
		else if (double.Abs(b) > double.Abs(a))
		{
			t = a / b;
			s = -1 / double.Sqrt((1 + t * t));
			c = -s * t;
		}
		else
		{
			t = b / a;
			c = 1 / double.Sqrt((1 + t * t));
			s = -c * t;
		}
		m_oJ[0, 0] = c; m_oJ[0, 1] = -s;
		m_oJ[1, 0] = s; m_oJ[1, 1] = c;
	}
	void PreMultiplyGivens(Matrix<double> matrix, int i, int j)
	{
		int rowSize = matrix.Columns;
		for (int row = 0; row < rowSize; ++row)
		{
			double tmp = matrix[i, row] * m_oJ[0, 0] + matrix[j, row] * m_oJ[0, 1];
			matrix[j, row] = matrix[i, row] * m_oJ[1, 0] + matrix[j, row] * m_oJ[1, 1];
			matrix[i, row] = tmp;
		}
	}
	public Matrix<double> Solve(Matrix<double> matrix)
	{
		Matrix<double> QtN = m_oQ.Transpose() * matrix;
		int cols = m_oR.Columns;
		Matrix<double> S = new(1, cols);
		for (int i = cols - 1; i >= 0; --i)
		{
			S[0, i] = QtN[i, 0];
			for (int j = i + 1; j < cols; ++j)
				S[0, i] -= S[0, j] * m_oR[i, j];
			S[0, i] /= m_oR[i, i];
		}
		return S;
	}
	public void Decompose(Matrix<double> matrix)
	{
		int rows = matrix.Rows;
		int cols = matrix.Columns;
		if (rows == cols)
			cols--;
		else if (rows < cols)
			cols = rows - 1;
		m_oQ = Matrix<double>.Identity(rows);
		m_oR = matrix;
		for (int j = 0; j < cols; ++j)
			for (int i = j + 1; i < rows; ++i)
			{
				GivensRotation(m_oR[j, j], m_oR[i, j]);
				PreMultiplyGivens(m_oR, j, i);
				PreMultiplyGivens(m_oR, j, i);
			}
		m_oQ = m_oQ.Transpose();
	}
	public Matrix<double> Inverse(Matrix<double> matrix)
	{
		Matrix<double> identity = Matrix<double>.Identity(matrix.Rows);
		Decompose(matrix);
		return Solve(identity);
	}
}