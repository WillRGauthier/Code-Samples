#pragma once
#include <array>
#include <stdexcept>
#include <memory>
#include "Math.h"
#include "Vector.h"
#include "Quaternion.h"

// This library follows the convention where possible that functions are defined twice:
//  once as a member function that acts in-place, and once as a free function that returns a new, altered copy
// The structs in this library only accept arithmetic template arguments
// Matrices are stored in row-major order because that is the memory layout for C/C++
// Matrices are intended to be used with column vectors (post-multiplied) regarding affine transformations
// Matrices' internal data is accessible as a public std::array called data
// There are using aliases for common matrix types and sizes at the end of the declarations
// Square matrices sized 2 and 3 have static constants of commonly useful defaults
// These functions, particularly rotation matrices, inverses, and determinants, are inspired heavily by the sample code from Essential Mathematics for Games and Interactive Applications
//  -https://github.com/jvanverth/essentialmath

// Forward declare interior namespace structs so they are seen as little as possible
namespace interior
{
	template<typename T>
	struct SizedMatrixOperator;
	template<typename T>
	struct SizedSquareMatrixOperator;
}
// Forward declare SquareMatrix so it can be used in a Matrix constructor
template<typename T, std::size_t size>
struct SquareMatrix;

// Generic matrix
template<typename T, std::size_t r, std::size_t c>
struct Matrix
{
public:
	static_assert(std::is_arithmetic<T>::value, "Matrices only accept arithmetic template arguments");

	std::array<T, r * c> data;

	// Default to a zero matrix
	constexpr Matrix();
	constexpr explicit Matrix(const T& fillVal);
	// initializer_list constructor sets missing arguments to 0 if there are fewer than r*c values and ignores extra values
	constexpr Matrix(std::initializer_list<T> args);
	constexpr explicit Matrix(const T* const rawOtherMat);
	constexpr Matrix(const Matrix<T, r, c>& other);

	constexpr Matrix<T, r, c>& operator=(const Matrix<T, r, c>& other);

	// Change this matrix to zero matrix in place
	Matrix<T, r, c>& Zero();

	T& operator()(std::size_t row, std::size_t col);
	const T& operator()(std::size_t row, std::size_t col) const;
	// Component-wise matrix +=
	Matrix<T, r, c>& operator+=(const Matrix<T, r, c>& rhs);
	// Component-wise matrix -=
	Matrix<T, r, c>& operator-=(const Matrix<T, r, c>& rhs);
	// Matrix multiplication *= only valid with a square cols x cols matrix
	Matrix<T, r, c>& operator*=(const Matrix<T, c, c>& rhs);
	// Scalar *=
	template<typename S>
	Matrix<T, r, c>& operator*=(const S& scalar);
	// Scalar /=
	template<typename S>
	Matrix<T, r, c>& operator/=(const S& scalar);

	// Starting from the first row sets as many rows as there are arguments, leaving any remaining rows unchanged
	void SetRows(std::initializer_list<Vector<T, c>> vecs);
	// Starting from the first column sets as many columns as there are arguments, leaving any remaining columns unchanged
	void SetCols(std::initializer_list<Vector<T, r>> vecs);

protected:
	std::unique_ptr<interior::SizedMatrixOperator<T>> matrixOp;

	// Constructors that accept an overloaded matrix operator
	constexpr Matrix(interior::SizedMatrixOperator<T>* operatorInstance);
	constexpr Matrix(const T& fillVal, interior::SizedMatrixOperator<T>* operatorInstance);
	constexpr Matrix(std::initializer_list<T> args, interior::SizedMatrixOperator<T>* operatorInstance);
	constexpr explicit Matrix(const T* const rawOtherMat, interior::SizedMatrixOperator<T>* operatorInstance);
	constexpr Matrix(const Matrix<T, r, c>& other, interior::SizedMatrixOperator<T>* operatorInstance);

	// Friend functions that need to access matrixOp
	template<typename U, std::size_t d1, std::size_t d2, std::size_t d3>
	friend Matrix<U, d1, d3> operator*(const Matrix<U, d1, d2>& lhs, const Matrix<U, d2, d3>& rhs);
	template<typename U, std::size_t d1, std::size_t d2>
	friend Vector<U, d1> operator*(const Matrix<U, d1, d2>& mat, const Vector<U, d2>& vec);
	template<typename U, std::size_t d>
	friend Vector<U, d> operator*(const SquareMatrix<U, d>& mat, const Vector<U, d>& vec);
	template<typename U, std::size_t d1, std::size_t d2>
	friend Vector<U, d2> operator*(const Vector<U, d1>& vec, const Matrix<U, d1, d2>& mat);
	template<typename U, std::size_t d>
	friend Vector<U, d> operator*(const Vector<U, d>& vec, const SquareMatrix<U, d>& mat);
	template<typename U, std::size_t d1, std::size_t d2>
	friend Matrix<U, d2, d1> Transpose(const Matrix<U, d1, d2>& mat);
};

// Generic square matrix
template<typename T, std::size_t size>
struct SquareMatrix : public Matrix<T, size, size>
{
	// Default to a zero matrix
	constexpr SquareMatrix();
	constexpr explicit SquareMatrix(const T& fillVal);
	// initializer_list constructor sets missing arguments to 0 if there are fewer than size^2 values and ignores extra values
	constexpr SquareMatrix(std::initializer_list<T> args);
	constexpr explicit SquareMatrix(const T* const rawOtherMat);
	constexpr SquareMatrix(const SquareMatrix<T, size>& other);
	// Enable implicit promotion to SquareMatrix from Matrix
	constexpr SquareMatrix(const Matrix<T, size, size>& other);

	constexpr SquareMatrix<T, size>& operator=(const SquareMatrix<T, size>& other);

	// Change this matrix to identity matrix in place
	SquareMatrix<T, size>& Identity();

	// Transpose this matrix in place
	SquareMatrix<T, size>& Transpose();
	// Invert this matrix in place using Gauss-Jordan elimination or do nothing if it is singular (non-invertible)
	SquareMatrix<T, size>& Inverse();

	// Compute the determinant using Gaussian elimination
	T Determinant() const;
	T Trace() const;
};

// Matrix 3x3 template specialization
template<typename T>
struct SquareMatrix<T, 3> : public Matrix<T, 3, 3>
{
	// Default to a zero matrix
	constexpr SquareMatrix();
	constexpr explicit SquareMatrix(const T& fillVal);
	constexpr SquareMatrix(T one, T two, T three,
							T four, T five, T six,
							T seven, T eight, T nine);
	constexpr explicit SquareMatrix(const T* const rawOtherMat);
	constexpr SquareMatrix(const SquareMatrix<T, 3>& other);
	// Enable implicit promotion to SquareMatrix from Matrix
	constexpr SquareMatrix(const Matrix<T, 3, 3>& other);

	constexpr SquareMatrix<T, 3>& operator=(const SquareMatrix<T, 3>& other);

	// Change this matrix to identity matrix in place
	SquareMatrix<T, 3>& Identity();

	// Transpose this matrix in place
	SquareMatrix<T, 3>& Transpose();
	// Invert this matrix in place using Cramer's method or do nothing if it is singular (non-invertible)
	SquareMatrix<T, 3>& Inverse();

	// Change this matrix into a scaling matrix with the given scale factors
	SquareMatrix<T, 3>& Scale(const Vector<T, 3>& scale);
	// Change this matrix into a rotating matrix from the given quaternion
	SquareMatrix<T, 3>& Rotation(const Quaternion<T>& quat);
	// Change this matrix into a rotating matrix by the given euler angles in radians
	SquareMatrix<T, 3>& Rotation(float yawZRot, float pitchYRot, float rollXRot);
	// Change this matrix into a rotating matrix about the given axis by the given angle in radians
	SquareMatrix<T, 3>& Rotation(const Vector<T, 3>& axis, float angle);
	// Change this matrix into a rotating matrix about the x axis by the given angle in radians
	SquareMatrix<T, 3>& RotationX(float angle);
	// Change this matrix into a rotating matrix about the y axis by the given angle in radians
	SquareMatrix<T, 3>& RotationY(float angle);
	// Change this matrix into a rotating matrix about the z axis by the given angle in radians
	SquareMatrix<T, 3>& RotationZ(float angle);

	T Determinant() const;
	T Trace() const;

	// Useful defaults
	static const SquareMatrix<T, 3> zero;
	static const SquareMatrix<T, 3> identity;
};

// Matrix 4x4 template specialization
template<typename T>
struct SquareMatrix<T, 4> : public Matrix<T, 4, 4>
{
	// Default to a zero matrix
	constexpr SquareMatrix();
	constexpr explicit SquareMatrix(const T& fillVal);
	constexpr SquareMatrix(T one, T two, T three, T four,
							T five, T six, T seven, T eight,
							T nine, T ten, T eleven, T twelve,
							T thirteen, T fourteen, T fifteen, T sixteen);
	constexpr explicit SquareMatrix(const T* const rawOtherMat);
	constexpr SquareMatrix(const SquareMatrix<T, 4>& other);
	// Enable implicit promotion to SquareMatrix from Matrix
	constexpr SquareMatrix(const Matrix<T, 4, 4>& other);

	constexpr SquareMatrix<T, 4>& operator=(const SquareMatrix<T, 4>& other);

	// Change this matrix to identity matrix in place
	SquareMatrix<T, 4>& Identity();

	// Transpose this matrix in place
	SquareMatrix<T, 4>& Transpose();
	// Invert this matrix in place assuming a standard affine matrix (bottom row is 0, 0, 0, 1) or do nothing if it is singular (non-invertible)
	SquareMatrix<T, 4>& AffineInverse();
	// Invert this matrix in place using Cramer's method or do nothing if it is singular (non-invertible)
	SquareMatrix<T, 4>& Inverse();

	// Change this matrix into a scaling affine matrix with the given scale factors
	SquareMatrix<T, 4>& Scale(const Vector<T, 3>& scale);
	// Change this matrix into a translating affine matrix with the given translation
	SquareMatrix<T, 4>& Translation(const Vector<T, 3>& translation);
	// Change this matrix into a rotating affine matrix from the given 3x3 rotation matrix
	SquareMatrix<T, 4>& Rotation(const SquareMatrix<T, 3>& mat);
	// Change this matrix into a rotating affine matrix from the given quaternion
	SquareMatrix<T, 4>& Rotation(const Quaternion<T>& quat);
	// Change this matrix into a rotating affine matrix by the given euler angles in radians
	SquareMatrix<T, 4>& Rotation(float yawZRot, float pitchYRot, float rollXRot);
	// Change this matrix into a rotating affine matrix about the given axis by the given angle in radians
	SquareMatrix<T, 4>& Rotation(const Vector<T, 3>& axis, float angle);
	// Change this matrix into a rotating affine matrix about the x axis by the given angle in radians
	SquareMatrix<T, 4>& RotationX(float angle);
	// Change this matrix into a rotating affine matrix about the y axis by the given angle in radians
	SquareMatrix<T, 4>& RotationY(float angle);
	// Change this matrix into a rotating affine matrix about the z axis by the given angle in radians
	SquareMatrix<T, 4>& RotationZ(float angle);


	T Determinant() const;
	T Trace() const;

	// Transform a column vector (post-multiply) with an implied 4th dimension w of 0
	Vector<T, 3> TransformVec(const Vector<T, 3>& vec) const;
	// Transform a column point (post-multiply) with an implied 4th dimension w of 1
	Vector<T, 3> TransformPoint(const Vector<T, 3>& point) const;

	// Useful defaults
	static const SquareMatrix<T, 4> zero;
	static const SquareMatrix<T, 4> identity;
};

// Matrix free functions
// "Constructor" from row vectors
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> MatrixFromRowVecs(std::initializer_list<Vector<T, c>> rowVecs);
// "Constructor" from column vectors
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> MatrixFromColVecs(std::initializer_list<Vector<T, r>> colVecs);

// Return zero matrix; Caller may have to assist compiler with type deduction by specifying type and size in angle brackets(i.e.Zero<int, 2, 3>())
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> Zero();
// Return identity matrix; Caller may have to assist compiler with type deduction by specifying type and size in angle brackets(i.e.Identity<int, 4>())
template<typename T, std::size_t size>
SquareMatrix<T, size> Identity();

// Component-wise matrix addition
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator+(const Matrix<T, r, c>& lhs, const Matrix<T, r, c>& rhs);
// Component-wise matrix subtraction
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator-(const Matrix<T, r, c>& lhs, const Matrix<T, r, c>& rhs);
// Matrix multiplication
template<typename T, std::size_t r, std::size_t c, std::size_t c2>
Matrix<T, r, c2> operator*(const Matrix<T, r, c>& lhs, const Matrix<T, c, c2>& rhs);
// Matrix multiplication for square matrices to avoid ambiguity between S and SquareMatrix
template<typename T, std::size_t size>
SquareMatrix<T, size> operator*(const SquareMatrix<T, size>& lhs, const SquareMatrix<T, size>&rhs);
// Scalar multiplication
template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator*(const Matrix<T, r, c>& mat, const S& scalar);
// Scalar multiplication
template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator*(const S& scalar, const Matrix<T, r, c>& mat);
// Scalar division
template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator/(const Matrix<T, r, c>& mat, const S& scalar);
// Negate unary -
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator-(const Matrix<T, r, c>& mat);

// Column vector multiplier
template<typename T, std::size_t r, std::size_t c>
Vector<T, r> operator*(const Matrix<T, r, c>& mat, const Vector<T, c>& vec);
// Column vector multiplier for square matrices to avoid ambiguity between S and SquareMatrix
template<typename T, std::size_t size>
Vector<T, size> operator*(const SquareMatrix<T, size>& mat, const Vector<T, size>& vec);
// Row vector multiplier
template<typename T, std::size_t r, std::size_t c>
Vector<T, c> operator*(const Vector<T, r>& vec, const Matrix<T, r, c>& mat);
// Row vector multiplier for square matrices to avoid ambiguity between S and SquareMatrix
template<typename T, std::size_t size>
Vector<T, size> operator*(const Vector<T, size>& vec, const SquareMatrix<T, size>& mat);

// Transpose a copy of mat
template<typename T, std::size_t r, std::size_t c>
Matrix<T, c, r> Transpose(const Matrix<T, r, c>& mat);
// Returns the inverse of mat or a copy of mat if it is singular (non-invertible) using Gauss-Jordan elimination
template<typename T, std::size_t size>
SquareMatrix<T, size> Inverse(const SquareMatrix<T, size>& mat);
// Returns the inverse of 4x4 matrix mat or a copy of mat if it is singular (non-invertible) assuming a standard affine matrix (bottom row is 0, 0, 0, 1)
template<typename T>
SquareMatrix<T, 4> AffineInverse(const SquareMatrix<T, 4>& mat);

// Return a 3x3 scaling matrix with the given scale factors
template<typename T>
SquareMatrix<T, 3> Scale(const Vector<T, 3>& scale);
// Return an affine scaling matrix with the given scale factors
template<typename T>
SquareMatrix<T, 4> Scale(const Vector<T, 3>& scale);
// Return an affine translating matrix with the given transformation
template<typename T>
SquareMatrix<T, 4> Translation(const Vector<T, 3>& transformation);
// Return an affine rotating matrix from the given 3x3 rotation matrix
template<typename T>
SquareMatrix<T, 4> Rotation(const SquareMatrix<T, 3>& mat);
// Return a 3x3 rotating matrix from the given quaternion
template<typename T>
SquareMatrix<T, 3> Rotation(const Quaternion<T>& quat);
// Return an affine rotating matrix from the given quaternion
template<typename T>
SquareMatrix<T, 4> Rotation(const Quaternion<T>& quat);
// Return a 3x3 rotating matrix by the given euler angles in radians
template<typename T>
SquareMatrix<T, 3> Rotation(float yawZRot, float pitchYRot, float rollXRot);
// Return an affine rotating matrix by the given euler angles in radians
template<typename T>
SquareMatrix<T, 4> Rotation(float yawZRot, float pitchYRot, float rollXRot);
// Return a 3x3 rotating matrix about the given axis by the given angle in radians
template<typename T>
SquareMatrix<T, 3> Rotation(const Vector<T, 3>& axis, float angle);
// Return an affine rotating matrix about the given axis by the given angle in radians
template<typename T>
SquareMatrix<T, 4> Rotation(const Vector<T, 3>& axis, float angle);
// Return a 3x3 rotating matrix about the x axis by the given angle in radians
template<typename T>
SquareMatrix<T, 3> RotationX(float angle);
// Return an affine rotating matrix about the x axis by the given angle in radians
template<typename T>
SquareMatrix<T, 4> RotationX(float angle);
// Return a 3x3 rotating matrix about the y axis by the given angle in radians
template<typename T>
SquareMatrix<T, 3> RotationY(float angle);
// Return an affine rotating matrix about the y axis by the given angle in radians
template<typename T>
SquareMatrix<T, 4> RotationY(float angle);
// Return a 3x3 rotating matrix about the z axis by the given angle in radians
template<typename T>
SquareMatrix<T, 3> RotationZ(float angle);
// Return an affine rotating matrix about the z axis by the given angle in radians
template<typename T>
SquareMatrix<T, 4> RotationZ(float angle);

// Compute the determinant using Gaussian elimination
template<typename T, std::size_t size>
T Determinant(const SquareMatrix<T, size>& mat);
template<typename T, std::size_t size>
T Trace(const SquareMatrix<T, size>& mat);

// Transform a column vector (post-multiply) with an implied 4th dimension w of 0
template<typename T>
Vector<T, 3> TransformVec(const SquareMatrix<T, 4>& mat, const Vector<T, 3>& vec);
// Transform a column point (post-multiply) with an implied 4th dimension w of 1
template<typename T>
Vector<T, 3> TransformPoint(const SquareMatrix<T, 4>& mat, const Vector<T, 3>& point);

// Common aliases
using float3x3 = SquareMatrix<float, 3>;
using float4x4 = SquareMatrix<float, 4>;
using double3x3 = SquareMatrix<double, 3>;
using double4x4 = SquareMatrix<double, 4>;

// Matrix operators that point to the data and perform functions on it without size templated to avoid code-bloated binaries due to matrices of the same type with different sized loops
// See Scott Meyers' Effective C++ Item 44
namespace interior
{
	template<typename T>
	struct SizedMatrixOperator
	{
	public:
		constexpr SizedMatrixOperator(std::size_t inRows, std::size_t inCols, T* pMem);
		constexpr SizedMatrixOperator(const SizedMatrixOperator<T>& op);

		void LoopedCopyOtherRaw(const T* const other);
		void FillFromInitializerList(std::initializer_list<T> args);

		T& operator()(std::size_t row, std::size_t col);
		const T& operator()(std::size_t row, std::size_t col) const;
		// Component-wise matrix +=
		void operator+=(const SizedMatrixOperator<T>& rhs);
		// Component-wise matrix -=
		void operator-=(const SizedMatrixOperator<T>& rhs);
		// Matrix multiplication *= only valid with a square cols x cols matrix
		void operator*=(const SizedMatrixOperator<T>& rhs);
		// Scalar *=
		template<typename S>
		void operator*=(const S& scalar);
		// Scalar /=
		template<typename S>
		void operator/=(const S& scalar);
		// Set this to the multiplication of lhs and rhs
		void MatrixMultiply(const SizedMatrixOperator<T>& lhs, const SizedMatrixOperator<T>& rhs);

		void Transpose(const T* const mat);

		void ColVecMult(T* retVec, const T* const vec);
		void RowVecMult(T* retVec, const T* const vec);

		std::size_t GetNumRows() const;
		std::size_t GetNumCols() const;

		void SetRow(std::size_t row, const T* const vec);
		void SetCol(std::size_t col, const T* const vec);

	protected:
		std::size_t rows, cols;
		T* pData;
	};

	template<typename T>
	struct SizedSquareMatrixOperator : public SizedMatrixOperator<T>
	{
		constexpr SizedSquareMatrixOperator(std::size_t size, T* pMem);
		constexpr SizedSquareMatrixOperator(const SizedMatrixOperator<T>& op);

		// Change this matrix into identity matrix in place
		void Identity();

		// Transpose this matrix in place
		void Transpose();
		// Inverts this matrix in place using Gauss-Jordan elimination and returns whether successful (destructive)
		bool TryInvert();	

		// Compute the determinant using Gaussian elimination (destructive)
		T Determinant() const;
		T Trace() const;
	};
}

// Implementations
// Generic matrix implementations
template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix()
	: data{}, matrixOp(new interior::SizedMatrixOperator<T>(r, c, data.data()))
{}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const T& fillVal)
	: matrixOp(new interior::SizedMatrixOperator<T>(r, c, data.data()))
{
	data.fill(fillVal);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(std::initializer_list<T> args)
	: matrixOp(new interior::SizedMatrixOperator<T>(r, c, data.data()))
{
	matrixOp->FillFromInitializerList(args);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const T* const rawOtherMat)
	: matrixOp(new interior::SizedMatrixOperator<T>(r, c, data.data()))
{
	matrixOp->LoopedCopyOtherRaw(rawOtherMat);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const Matrix<T, r, c>& other)
	: matrixOp(new interior::SizedMatrixOperator<T>(r, c, data.data()))
{
	data = other.data;
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>& Matrix<T, r, c>::operator=(const Matrix<T, r, c>& other)
{
	data = other.data;
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c>& Matrix<T, r, c>::Zero()
{
	data.fill(0);
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
T& Matrix<T, r, c>::operator()(std::size_t row, std::size_t col)
{
	return matrixOp(row, col);
}

template<typename T, std::size_t r, std::size_t c>
const T& Matrix<T, r, c>::operator()(std::size_t row, std::size_t col) const
{
	return matrixOp(row, col);
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c>& Matrix<T, r, c>::operator+=(const Matrix<T, r, c>& rhs)
{
	*matrixOp += *(rhs.matrixOp);
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c>& Matrix<T, r, c>::operator-=(const Matrix<T, r, c>& rhs)
{
	*matrixOp -= *(rhs.matrixOp);
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c>& Matrix<T, r, c>::operator*=(const Matrix<T, c, c>& rhs)
{
	*matrixOp *= *(rhs.matrixOp);
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
template<typename S>
Matrix<T, r, c>& Matrix<T, r, c>::operator*=(const S& scalar)
{
	*matrixOp *= scalar;
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
template<typename S>
Matrix<T, r, c>& Matrix<T, r, c>::operator/=(const S& scalar)
{
	*matrixOp /= scalar;
	return *this;
}

template<typename T, std::size_t r, std::size_t c>
void Matrix<T, r, c>::SetRows(std::initializer_list<Vector<T, c>> vecs)
{
	std::size_t passedInValsCount = std::min(matrixOp->GetNumRows(), vecs.size());
	std::size_t row = 0;
	for (const Vector<T, c>& vec : vecs)
	{
		if (row < passedInValsCount)
		{
			matrixOp->SetRow(row, vec.data.data());
			++row;
		}
		else
		{
			break;
		}
	}
}

template<typename T, std::size_t r, std::size_t c>
void Matrix<T, r, c>::SetCols(std::initializer_list<Vector<T, r>> vecs)
{
	std::size_t passedInValsCount = std::min(matrixOp->GetNumCols(), vecs.size());
	std::size_t col = 0;
	for (const Vector<T, r>& vec : vecs)
	{
		if (col < passedInValsCount)
		{
			matrixOp->SetCol(col, vec.data.data());
			++col;
		}
		else
		{
			break;
		}
	}
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(interior::SizedMatrixOperator<T>* operatorInstance)
	: data{}, matrixOp(operatorInstance)
{}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const T& fillVal, interior::SizedMatrixOperator<T>* operatorInstance)
	: matrixOp(operatorInstance)
{
	data.fill(fillVal);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(std::initializer_list<T> args, interior::SizedMatrixOperator<T>* operatorInstance)
	: matrixOp(operatorInstance)
{
	matrixOp->FillFromInitializerList(args);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const T* const rawOtherMat, interior::SizedMatrixOperator<T>* operatorInstance)
	: matrixOp(operatorInstance)
{
	matrixOp->LoopedCopyOtherRaw(rawOtherMat);
}

template<typename T, std::size_t r, std::size_t c>
constexpr Matrix<T, r, c>::Matrix(const Matrix<T, r, c>& other, interior::SizedMatrixOperator<T>* operatorInstance)
	: matrixOp(operatorInstance)
{
	data = other.data;
}

// SquareMatrix unspecialized implementations
template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix()
	: Matrix<T, size, size>(new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix(const T& fillVal)
	: Matrix<T, size, size>(fillVal, new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix(std::initializer_list<T> args)
	: Matrix<T, size, size>(args, new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix(const T* const rawOtherMat)
	: Matrix<T, size, size>(rawOtherMat, new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix(const SquareMatrix<T, size>& other)
	: Matrix<T, size, size>(other, new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>::SquareMatrix(const Matrix<T, size, size>& other)
	: Matrix<T, size, size>(other, new interior::SizedSquareMatrixOperator<T>(size, data.data()))
{}

template<typename T, std::size_t size>
constexpr SquareMatrix<T, size>& SquareMatrix<T, size>::operator=(const SquareMatrix<T, size>& other)
{
	data = other.data;
	return *this;
}

template<typename T, std::size_t size>
SquareMatrix<T, size>& SquareMatrix<T, size>::Identity()
{
	static_cast<interior::SizedSquareMatrixOperator<T>>(*matrixOp).Identity();
	return *this;
}

template<typename T, std::size_t size>
SquareMatrix<T, size>& SquareMatrix<T, size>::Transpose()
{
	static_cast<interior::SizedSquareMatrixOperator<T>>(*matrixOp).Transpose();
	return *this;
}

template<typename T, std::size_t size>
SquareMatrix<T, size>& SquareMatrix<T, size>::Inverse()
{
	SquareMatrix<T, size> tempCopy(*this);
	if (static_cast<interior::SizedSquareMatrixOperator<T>>(*tempCopy.matrixOp).TryInvert())
	{
		*this = tempCopy;
	}
	return *this;
}

template<typename T, std::size_t size>
T SquareMatrix<T, size>::Determinant() const
{
	SquareMatrix<T, size> tempCopy(*this);
	return static_cast<interior::SizedSquareMatrixOperator<T>>(*tempCopy.matrixOp).Determinant();
}

template<typename T, std::size_t size>
T SquareMatrix<T, size>::Trace() const
{
	return static_cast<interior::SizedSquareMatrixOperator<T>>(*matrixOp).Trace();
}

// Matrix 3x3 template specialization implementations
template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix()
	: Matrix<T, 3, 3>(new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix(const T& fillVal)
	: Matrix<T, 3, 3>(fillVal, new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix(T one, T two, T three,
											T four, T five, T six,
											T seven, T eight, T nine)
	: Matrix<T, 3, 3>({ one, two, three, four, five, six, seven, eight, nine }, new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix(const T* const rawOtherMat)
	: Matrix<T, 3, 3>(rawOtherMat, new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix(const SquareMatrix<T, 3>& other)
	: Matrix<T, 3, 3>(other, new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 3>::SquareMatrix(const Matrix<T, 3, 3>& other)
	: Matrix<T, 3, 3>(other, new interior::SizedSquareMatrixOperator<T>(3, data.data()))
{}


template<typename T>
constexpr SquareMatrix<T, 3>& SquareMatrix<T, 3>::operator=(const SquareMatrix<T, 3>& other)
{
	data = other.data;
	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Identity()
{
	data[0] = 1; data[1] = 0; data[2] = 0;
	data[3] = 0; data[4] = 1; data[5] = 0;
	data[6] = 0; data[7] = 0; data[8] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Transpose()
{
	T temp = data[1];
	data[1] = data[3];
	data[3] = temp;

	temp = data[2];
	data[2] = data[6];
	data[6] = temp;

	temp = data[5];
	data[5] = data[7];
	data[7] = temp;

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Inverse()
{
	T cofactor00 = data[4] * data[8] - data[5] * data[7];
	T cofactor01 = data[5] * data[6] - data[3] * data[8];
	T cofactor02 = data[3] * data[7] - data[4] * data[6];
	float det = static_cast<float>(data[0] * cofactor00 + data[1] * cofactor01 + data[2] * cofactor02);
	if (!Math::IsZero(det))
	{
		SquareMatrix<T, 3> copy(*this);

		// Move along columns because adjoint is transpose of cofactors
		det = 1.0f / det;
		data[0] = det * cofactor00;
		data[3] = det * cofactor01;
		data[6] = det * cofactor02;
		data[1] = det * (copy.data[2] * copy.data[7] - copy.data[1] * copy.data[8]);
		data[4] = det * (copy.data[0] * copy.data[8] - copy.data[2] * copy.data[6]);
		data[7] = det * (copy.data[1] * copy.data[6] - copy.data[0] * copy.data[7]);
		data[2] = det * (copy.data[0] * copy.data[4] - copy.data[1] * copy.data[3]);
		data[5] = det * (copy.data[2] * copy.data[3] - copy.data[0] * copy.data[5]);
		data[8] = det * (copy.data[0] * copy.data[4] - copy.data[1] * copy.data[3]);
	}

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Scale(const Vector<T, 3>& scale)
{
	data[0] = scale.data[0]; data[1] = 0; data[2] = 0;
	data[3] = 0; data[4] = scale.data[1]; data[5] = 0;
	data[6] = 0; data[7] = 0; data[8] = scale.data[2];

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Rotation(const Quaternion<T>& quat)
{
	float s, sxx, syy, szz, sxy, sxz, syz, swx, swy, swz;

	// S = 2.0f if quat is normalized
	s = 2.0f / (quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);

	sxx = s * quat.x * quat.x;
	syy = s * quat.y * quat.y;
	szz = s * quat.z * quat.z;
	sxy = s * quat.x * quat.y;
	sxz = s * quat.x * quat.z;
	syz = s * quat.y * quat.z;
	swx = s * quat.w * quat.x;
	swy = s * quat.w * quat.y;
	swz = s * quat.w * quat.z;

	data[0] = 1 - syy - szz; data[1] = sxy - swz; data[2] = sxz + swy;
	data[3] = sxy + swz; data[4] = 1 - sxx - szz; data[5] = syz - swx;
	data[6] = sxz - swy; data[7] = syz + swx; data[8] = 1 - sxx - syy;

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Rotation(float yawZRot, float pitchYRot, float rollXRot)
{
	float cx, cy, cz, sx, sy, sz;

	cx = cos(rollXRot);
	cy = cos(pitchYRot);
	cz = cos(yawZRot);
	sx = sin(rollXRot);
	sy = sin(pitchYRot);
	sz = sin(yawZRot);

	data[0] = cy * cz; data[1] = -cy * sz; data[2] = sy;
	data[3] = sx * sy * cz + cx * sz; data[4] = -sx * sy * sz + cx * cz; data[5] = -sx * cy;
	data[6] = -cx * sy * cz + sx * sz; data[7] = cx * sy * sz + sx * cz; data[8] = cx * cy

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::Rotation(const Vector<T, 3>& axis, float angle)
{
	float c = cos(angle);
	float s = sin(angle);
	float a = 1.0f - c;

	Vector<T, 3> normalizedAxis(axis);
	normalizedAxis.Normalize();

	float ax = a * normalizedAxis.data[0];
	float ay = a * normalizedAxis.data[1];
	float az = a * normalizedAxis.data[2];
	float axy = ax * normalizedAxis.data[1];
	float axz = ax * normalizedAxis.data[2];
	float ayz = ay * normalizedAxis.data[2];
	float sx = s * normalizedAxis.data[0];
	float sy = s * normalizedAxis.data[1];
	float sz = s * normalizedAxis.data[2];

	data[0] = ax * normalizedAxis.data[0] + c; data[1] = axy - sz; data[2] = axz + sy;
	data[3] = axy + sz; data[4] = ay * normalizedAxis.data[1] + c; data[5] = ayz - sx;
	data[6] = axz - sy; data[7] = ayz + sx; data[8] = az * normalizedAxis.data[2] + c;
	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::RotationX(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = 1; data[1] = 0; data[2] = 0;
	data[3] = 0; data[4] = c; data[5] = -s;
	data[6] = 0; data[7] = s; data[8] = c;

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::RotationY(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = c; data[1] = 0; data[2] = s;
	data[3] = 0; data[4] = 1; data[5] = 0;
	data[6] = -s; data[7] = 0; data[8] = c;

	return *this;
}

template<typename T>
SquareMatrix<T, 3>& SquareMatrix<T, 3>::RotationZ(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = c; data[1] = -s; data[2] = 0;
	data[3] = s; data[4] = c; data[5] = 0;
	data[6] = 0; data[7] = 0; data[8] = 1;

	return *this;
}

template<typename T>
T SquareMatrix<T, 3>::Determinant() const
{
	return data[0] * (data[4] * data[8] - data[5] * data[7])
		- data[1] * (data[3] * data[8] - data[5] * data[6])
		+ data[2] * (data[3] * data[7] - data[4] * data[6]);
}

template<typename T>
T SquareMatrix<T, 3>::Trace() const
{
	return data[0] + data[4] + data[8];
}

template<typename T>
const SquareMatrix<T, 3> SquareMatrix<T, 3>::zero(0);
template<typename T>
const SquareMatrix<T, 3> SquareMatrix<T, 3>::identity(1, 0, 0,
													  0, 1, 0,
													  0, 0, 1);

// Matrix 4x4 template specialization implementations
template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix()
	: Matrix<T, 4, 4>(new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix(const T& fillVal)
	: Matrix<T, 4, 4>(fillVal, new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix(T one, T two, T three, T four,
											T five, T six, T seven, T eight,
											T nine, T ten, T eleven, T twelve,
											T thirteen, T fourteen, T fifteen, T sixteen)
	: Matrix<T, 4, 4>({ one, two, three, four, five, six, seven, eight, 
						nine, ten, eleven, twelve, thirteen, fourteen, fifteen, sixteen }, new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix(const T* const rawOtherMat)
	: Matrix<T, 4, 4>(rawOtherMat, new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix(const SquareMatrix<T, 4>& other)
	: Matrix<T, 4, 4>(other, new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>::SquareMatrix(const Matrix<T, 4, 4>& other)
	: Matrix<T, 4, 4>(other, new interior::SizedSquareMatrixOperator<T>(4, data.data()))
{}

template<typename T>
constexpr SquareMatrix<T, 4>& SquareMatrix<T, 4>::operator=(const SquareMatrix<T, 4>& other)
{
	data = other.data;
	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Identity()
{
	data[0] = 1; data[1] = 0; data[2] = 0; data[3] = 0;
	data[4] = 0; data[5] = 1; data[6] = 0; data[7] = 0;
	data[8] = 0; data[9] = 0; data[10] = 1; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Transpose()
{
	T temp = data[1];
	data[1] = data[4];
	data[4] = temp;

	temp = data[2];
	data[2] = data[8];
	data[8] = temp;

	temp = data[3];
	data[3] = data[12];
	data[12] = temp;

	temp = data[6];
	data[6] = data[9];
	data[9] = temp;

	temp = data[7];
	data[7] = data[13];
	data[13] = temp;

	temp = data[11];
	data[11] = data[14];
	data[14] = temp;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::AffineInverse()
{
	// Compute upper left 3x3 matrix determinant
	T cofactor00 = data[5] * data[10] - data[6] * data[9];
	T cofactor01 = data[6] * data[8] - data[4] * data[10];
	T cofactor02 = data[4] * data[9] - data[5] * data[8];
	float det = static_cast<float>(data[0] * cofactor00 + data[1] * cofactor01 + data[2] * cofactor02);
	if (!Math::IsZero(det))
	{
		SquareMatrix<T, 4> copy(*this);

		// Create adjunct matrix and multiply by 1/det to get upper 3x3
		det = 1.0f / det;
		data[0] = det * cofactor00;
		data[4] = det * cofactor01;
		data[8] = det * cofactor02;
		data[1] = det * (copy.data[2] * copy.data[9] - copy.data[1] * copy.data[10]);
		data[5] = det * (copy.data[0] * copy.data[10] - copy.data[2] * copy.data[8]);
		data[9] = det * (copy.data[1] * copy.data[8] - copy.data[0] * copy.data[9]);
		data[2] = det * (copy.data[1] * copy.data[6] - copy.data[2] * copy.data[5]);
		data[6] = det * (copy.data[2] * copy.data[4] - copy.data[0] * copy.data[6]);
		data[10] = det * (copy.data[0] * copy.data[5] - copy.data[1] * copy.data[4]);

		// Multiply negative translation by inverted upper 3x3
		data[3] = -data[0] * data[3] - data[1] * data[7] - data[2] * data[11];
		data[7] = -data[4] * data[3] - data[5] * data[7] - data[6] * data[11];
		data[11] = -data[8] * data[3] - data[9] * data[7] - data[10] * data[11];
	}

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Inverse()
{
	T cofactor00 = data[5] * (data[10] * data[15] - data[11] * data[14]) - data[6] * (data[9] * data[15] - data[11] * data[13]) + data[7] * (data[9] * data[14] - data[10] * data[13]);
	T cofactor01 = -(data[4] * (data[10] * data[15] - data[11] * data[14]) - data[6] * (data[8] * data[15] - data[11] * data[12]) + data[7] * (data[8] * data[14] - data[10] * data[12]));
	T cofactor02 = data[4] * (data[9] * data[15] - data[11] * data[13]) - data[5] * (data[8] * data[15] - data[11] * data[12]) + data[7] * (data[8] * data[13] - data[9] * data[12]);
	T cofactor03 = -(data[4] * (data[9] * data[14] - data[10] * data[13]) - data[5] * (data[8] * data[14] - data[10] * data[12]) + data[6] * (data[8] * data[13] - data[9] * data[12]));
	float det = static_cast<float>(data[0] * cofactor00 + data[1] * cofactor01 + data[2] * cofactor02 + data[3] * cofactor03);
	if (!Math::IsZero(det))
	{
		SquareMatrix<T, 4> copy(*this);

		det = 1.0f / det;
		data[0] = det * cofactor00;
		data[4] = det * cofactor01;
		data[8] = det * cofactor02;
		data[12] = det * cofactor03;
		data[1] = det * -(copy.data[1] * (copy.data[10] * copy.data[15] - copy.data[11] * copy.data[14]) - copy.data[2] * (copy.data[9] * copy.data[15] - copy.data[11] * copy.data[13]) + copy.data[3] * (copy.data[9] * copy.data[14] - copy.data[10] * copy.data[13]));
		data[5] = det * (copy.data[0] * (copy.data[10] * copy.data[15] - copy.data[11] * copy.data[14]) - copy.data[2] * (copy.data[8] * copy.data[15] - copy.data[11] * copy.data[12]) + copy.data[3] * (copy.data[8] * copy.data[14] - copy.data[10] * copy.data[12]));
		data[9] = det * -(copy.data[0] * (copy.data[9] * copy.data[15] - copy.data[11] * copy.data[13]) - copy.data[1] * (copy.data[8] * copy.data[15] - copy.data[11] * copy.data[12]) + copy.data[3] * (copy.data[8] * copy.data[13] - copy.data[9] * copy.data[12]));
		data[13] = det * (copy.data[0] * (copy.data[9] * copy.data[14] - copy.data[10] * copy.data[13]) - copy.data[1] * (copy.data[8] * copy.data[14] - copy.data[10] * copy.data[12]) + copy.data[2] * (copy.data[8] * copy.data[13] - copy.data[9] * copy.data[12]));
		data[2] = det * (copy.data[1] * (copy.data[6] * copy.data[15] - copy.data[7] * copy.data[14]) - copy.data[2] * (copy.data[5] * copy.data[15] - copy.data[7] * copy.data[13]) + copy.data[3] * (copy.data[5] * copy.data[14] - copy.data[6] * copy.data[13]));
		data[6] = det * -(copy.data[0] * (copy.data[6] * copy.data[15] - copy.data[7] * copy.data[14]) - copy.data[2] * (copy.data[4] * copy.data[15] - copy.data[7] * copy.data[12]) + copy.data[3] * (copy.data[4] * copy.data[14] - copy.data[6] * copy.data[12]));
		data[10] = det * (copy.data[0] * (copy.data[5] * copy.data[15] - copy.data[7] * copy.data[13]) - copy.data[1] * (copy.data[4] * copy.data[15] - copy.data[7] * copy.data[12]) + copy.data[3] * (copy.data[4] * copy.data[13] - copy.data[5] * copy.data[12]));
		data[14] = det * -(copy.data[0] * (copy.data[5] * copy.data[14] - copy.data[6] * copy.data[13]) - copy.data[1] * (copy.data[4] * copy.data[14] - copy.data[6] * copy.data[12]) + copy.data[2] * (copy.data[4] * copy.data[13] - copy.data[5] * copy.data[12]));
		data[3] = det * -(copy.data[1] * (copy.data[6] * copy.data[11] - copy.data[7] * copy.data[10]) - copy.data[2] * (copy.data[5] * copy.data[11] - copy.data[7] * copy.data[9]) + copy.data[3] * (copy.data[5] * copy.data[10] - copy.data[6] * copy.data[9]));
		data[7] = det * (copy.data[0] * (copy.data[6] * copy.data[11] - copy.data[7] * copy.data[10]) - copy.data[2] * (copy.data[4] * copy.data[11] - copy.data[7] * copy.data[8]) + copy.data[3] * (copy.data[4] * copy.data[10] - copy.data[6] * copy.data[8]));
		data[11] = det * -(copy.data[0] * (copy.data[5] * copy.data[11] - copy.data[7] * copy.data[9]) - copy.data[1] * (copy.data[4] * copy.data[11] - copy.data[7] * copy.data[8]) + copy.data[3] * (copy.data[4] * copy.data[9] - copy.data[5] * copy.data[8]));
		data[15] = det * (copy.data[0] * (copy.data[5] * copy.data[10] - copy.data[6] * copy.data[9]) - copy.data[1] * (copy.data[4] * copy.data[10] - copy.data[6] * copy.data[8]) + copy.data[2] * (copy.data[4] * copy.data[9] - copy.data[5] * copy.data[8]));
	}

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Scale(const Vector<T, 3>& scale)
{
	data[0] = scale.data[0]; data[1] = 0; data[2] = 0; data[3] = 0;
	data[4] = 0; data[5] = scale.data[1]; data[6] = 0; data[7] = 0;
	data[8] = 0; data[9] = 0; data[10] = scale.data[2]; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Translation(const Vector<T, 3>& transformation)
{
	data[0] = 1; data[1] = 0; data[2] = 0; data[3] = transformation.data[0];
	data[4] = 0; data[5] = 1; data[6] = 0; data[7] = transformation.data[1];
	data[8] = 0; data[9] = 0; data[10] = 1; data[11] = transformation.data[2];
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Rotation(const SquareMatrix<T, 3>& mat)
{
	data[0] = mat.data[0]; data[1] = mat.data[1]; data[2] = mat.data[2]; data[3] = 0;
	data[4] = mat.data[3]; data[5] = mat.data[4]; data[6] = mat.data[5]; data[7] = 0;
	data[8] = mat.data[6]; data[9] = mat.data[7]; data[10] = mat.data[8]; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Rotation(const Quaternion<T>& quat)
{
	float s, sxx, syy, szz, sxy, sxz, syz, swx, swy, swz;

	// S = 2.0f if quat is normalized
	s = 2.0f / (quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);

	sxx = s * quat.x * quat.x;
	syy = s * quat.y * quat.y;
	szz = s * quat.z * quat.z;
	sxy = s * quat.x * quat.y;
	sxz = s * quat.x * quat.z;
	syz = s * quat.y * quat.z;
	swx = s * quat.w * quat.x;
	swy = s * quat.w * quat.y;
	swz = s * quat.w * quat.z;

	data[0] = 1 - syy - szz; data[1] = sxy - swz; data[2] = sxz + swy; data[3] = 0;
	data[4] = sxy + swz; data[5] = 1 - sxx - szz; data[6] = syz - swx; data[7] = 0;
	data[8] = sxz - swy; data[9] = syz + swx; data[10] = 1 - sxx - syy; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Rotation(float yawZRot, float pitchYRot, float rollXRot)
{
	float cx, cy, cz, sx, sy, sz;

	cx = cos(rollXRot);
	cy = cos(pitchYRot);
	cz = cos(yawZRot);
	sx = sin(rollXRot);
	sy = sin(pitchYRot);
	sz = sin(yawZRot);

	data[0] = cy * cz; data[1] = -cy * sz; data[2] = sy; data[3] = 0;
	data[4] = sx * sy * cz + cx * sz; data[5] = -sx * sy * sz + cx * cz; data[6] = -sx * cy; data[7] = 0;
	data[8] = -cx * sy * cz + sx * sz; data[9] = cx * sy * sz + sx * cz; data[10] = cx * cy; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::Rotation(const Vector<T, 3>& axis, float angle)
{
	float c = cos(angle);
	float s = sin(angle);
	float a = 1.0f - c;

	Vector<T, 3> normalizedAxis(axis);
	normalizedAxis.Normalize();

	float ax = a * normalizedAxis.data[0];
	float ay = a * normalizedAxis.data[1];
	float az = a * normalizedAxis.data[2];
	float axy = ax * normalizedAxis.data[1];
	float axz = ax * normalizedAxis.data[2];
	float ayz = ay * normalizedAxis.data[2];
	float sx = s * normalizedAxis.data[0];
	float sy = s * normalizedAxis.data[1];
	float sz = s * normalizedAxis.data[2];

	data[0] = ax * normalizedAxis.data[0] + c; data[1] = axy - sz; data[2] = axz + sy; data[3] = 0;
	data[4] = axy + sz; data[5] = ay * normalizedAxis.data[1] + c; data[6] = ayz - sx; data[7] = 0;
	data[8] = axz - sy; data[9] = ayz + sx; data[10] = az * normalizedAxis.data[2] + c; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::RotationX(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = 1; data[1] = 0; data[2] = 0; data[3] = 0;
	data[4] = 0; data[5] = c; data[6] = -s; data[7] = 0;
	data[8] = 0; data[9] = s; data[10] = c; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::RotationY(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = c; data[1] = 0; data[2] = s; data[3] = 0;
	data[4] = 0; data[5] = 1; data[6] = 0; data[7] = 0;
	data[8] = -s; data[9] = 0; data[10] = c; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
SquareMatrix<T, 4>& SquareMatrix<T, 4>::RotationZ(float angle)
{
	float c = cos(angle);
	float s = sin(angle);

	data[0] = c; data[1] = -s; data[2] = 0; data[3] = 0;
	data[4] = s; data[5] = c; data[6] = 0; data[7] = 0;
	data[8] = 0; data[9] = 0; data[10] = 1; data[11] = 0;
	data[12] = 0; data[13] = 0; data[14] = 0; data[15] = 1;

	return *this;
}

template<typename T>
T SquareMatrix<T, 4>::Determinant() const
{
	return data[0] * (data[5] * (data[10] * data[15] - data[11] * data[14]) - data[6] * (data[9] * data[15] - data[11] * data[13]) + data[7] * (data[9] * data[14] - data[10] * data[13]))
		- data[1] * (data[4] * (data[10] * data[15] - data[11] * data[14]) - data[6] * (data[8] * data[15] - data[11] * data[12]) + data[7] * (data[8] * data[14] - data[10] * data[12]))
		+ data[2] * (data[4] * (data[9] * data[15] - data[11] * data[13]) - data[5] * (data[8] * data[15] - data[11] * data[12]) + data[7] * (data[8] * data[13] - data[9] * data[12]))
		- data[3] * (data[4] * (data[9] * data[14] - data[10] * data[13]) - data[5] * (data[8] * data[14] - data[10] * data[12]) + data[6] * (data[8] * data[13] - data[9] * data[12]));
}

template<typename T>
T SquareMatrix<T, 4>::Trace() const
{
	return data[0] + data[5] + data[10] + data[15];
}

template<typename T>
Vector<T, 3> SquareMatrix<T, 4>::TransformVec(const Vector<T, 3>& vec) const
{
	Vector<T, 3> result;
	result[0] = data[0] * vec.data[0] + data[1] * vec.data[0] + data[2] * vec.data[0];
	result[1] = data[4] * vec.data[1] + data[5] * vec.data[1] + data[6] * vec.data[1];
	result[2] = data[8] * vec.data[2] + data[9] * vec.data[2] + data[10] * vec.data[2];
	return result;
}

template<typename T>
Vector<T, 3> SquareMatrix<T, 4>::TransformPoint(const Vector<T, 3>& point) const
{
	Vector<T, 3> result;
	result[0] = data[0] * vec.data[0] + data[1] * vec.data[0] + data[2] * vec.data[0] + data[3];
	result[1] = data[4] * vec.data[1] + data[5] * vec.data[1] + data[6] * vec.data[1] + data[7];
	result[2] = data[8] * vec.data[2] + data[9] * vec.data[2] + data[10] * vec.data[2] + data[11];
	return result;
}

template<typename T>
const SquareMatrix<T, 4> SquareMatrix<T, 4>::zero(0, 0, 0, 0);
template<typename T>
const SquareMatrix<T, 4> SquareMatrix<T, 4>::identity(1, 0, 0, 0,
													  0, 1, 0, 0,
													  0, 0, 1, 0,
													  0, 0, 0, 1);

// Matrix free functions implementations
template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> MatrixFromRowVecs(std::initializer_list<Vector<T, c>> rowVecs)
{
	Matrix<T, r, c> retMat;
	retMat.SetRows(rowVecs);
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> MatrixFromColVecs(std::initializer_list<Vector<T, r>> colVecs)
{
	Matrix<T, r, c> retMat;
	retMat.SetCols(colVecs);
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> Zero()
{
	Matrix<T, r, c> retMat;
	return retMat;
}

template<typename T, std::size_t size>
SquareMatrix<T, size> Identity()
{
	SquareMatrix<T, size> retMat;
	retMat.Identity();
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator+(const Matrix<T, r, c>& lhs, const Matrix<T, r, c>& rhs)
{
	Matrix<T, r, c> retMat(lhs);
	retMat += rhs;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator-(const Matrix<T, r, c>& lhs, const Matrix<T, r, c>& rhs)
{
	Matrix<T, r, c> retMat(lhs);
	retMat -= rhs;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c, std::size_t c2>
Matrix<T, r, c2> operator*(const Matrix<T, r, c>& lhs, const Matrix<T, c, c2>& rhs)
{
	Matrix<T, r, c2> retMat;
	retMat.matrixOp->MatrixMultiply(*(lhs.matrixOp), *(rhs.matrixOp));
	return retMat;
}

template<typename T, std::size_t size>
SquareMatrix<T, size> operator*(const SquareMatrix<T, size>& lhs, const SquareMatrix<T, size>& rhs)
{
	SquareMatrix<T, size> retMat(lhs);
	static_cast<Matrix<T, size, size>&>(retMat) *= static_cast<const Matrix<T, size, size>&>(rhs);
	return retMat;
}

template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator*(const Matrix<T, r, c>& mat, const S& scalar)
{
	Matrix<T, r, c> retMat(mat);
	retMat *= scalar;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator*(const S& scalar, const Matrix<T, r, c>& mat)
{
	Matrix<T, r, c> retMat(mat);
	retMat *= scalar;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c, typename S>
Matrix<T, r, c> operator/(const Matrix<T, r, c>& mat, const S& scalar)
{
	Matrix<T, r, c> retMat(mat);
	retMat /= scalar;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, r, c> operator-(const Matrix<T, r, c>& mat)
{
	Matrix<T, r, c> retMat(mat);
	retMat *= -1;
	return retMat;
}

template<typename T, std::size_t r, std::size_t c>
Vector<T, r> operator*(const Matrix<T, r, c>& mat, const Vector<T, c>& vec)
{
	Vector<T, r> retVec;
	mat.matrixOp->ColVecMult(retVec.data.data(), vec.data.data());
	return retVec;
}

template<typename T, std::size_t size>
Vector<T, size> operator*(const SquareMatrix<T, size>& mat, const Vector<T, size>& vec)
{
	Vector<T, size> retVec;
	mat.matrixOp->ColVecMult(retVec.data.data(), vec.data.data());
	return retVec;
}

template<typename T, std::size_t r, std::size_t c>
Vector<T, c> operator*(const Vector<T, r>& vec, const Matrix<T, r, c>& mat)
{
	Vector<T, c> retVec;
	mat.matrixOp->RowVecMult(retVec.data.data(), vec.data.data());
	return retVec;
}

template<typename T, std::size_t size>
Vector<T, size> operator*(const Vector<T, size>& vec, const SquareMatrix<T, size>& mat)
{
	Vector<T, size> retVec;
	mat.matrixOp->RowVecMult(retVec.data.data(), vec.data.data());
	return retVec;
}

template<typename T, std::size_t r, std::size_t c>
Matrix<T, c, r> Transpose(const Matrix<T, r, c>& mat)
{
	Matrix<T, c, r> retMat;
	retMat.matrixOp->Transpose(mat.data.data());
	return retMat;
}

template<typename T, std::size_t size>
SquareMatrix<T, size> Inverse(const SquareMatrix<T, size>& mat)
{
	SquareMatrix<T, size> retMat(mat);
	retMat.Inverse();
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> AffineInverse(const SquareMatrix<T, 4>& mat)
{
	SquareMatrix<T, 4> retMat(mat);
	retMat.AffineInverse();
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> Scale(const Vector<T, 3>& scale)
{
	SquareMatrix<T, 3> retMat;
	retMat.Scale(scale);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Scale(const Vector<T, 3>& scale)
{
	SquareMatrix<T, 4> retMat;
	retMat.Scale(scale);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Translation(const Vector<T, 3>& transformation)
{
	SquareMatrix<T, 4> retMat;
	retMat.Translation(transformation);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Rotation(const SquareMatrix<T, 3>& mat)
{
	SquareMatrix<T, 4> retMat;
	retMat.Rotation(mat);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> Rotation(const Quaternion<T>& quat)
{
	SquareMatrix<T, 3> retMat;
	retMat.Rotation(quat);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Rotation(const Quaternion<T>& quat)
{
	SquareMatrix<T, 4> retMat;
	retMat.Rotation(quat);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> Rotation(float yawZRot, float pitchYRot, float rollXRot)
{
	SquareMatrix<T, 3> retMat;
	retMat.Rotation(yawZRot, pitchYRot, rollXRot);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Rotation(float yawZRot, float pitchYRot, float rollXRot)
{
	SquareMatrix<T, 4> retMat;
	retMat.Rotation(yawZRot, pitchYRot, rollXRot);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> Rotation(const Vector<T, 3>& axis, float angle)
{
	SquareMatrix<T, 3> retMat;
	retMat.Rotation(axis, angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> Rotation(const Vector<T, 3>& axis, float angle)
{
	SquareMatrix<T, 4> retMat;
	retMat.Rotation(axis, angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> RotationX(float angle)
{
	SquareMatrix<T, 3> retMat;
	retMat.RotationX(angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> RotationX(float angle)
{
	SquareMatrix<T, 4> retMat;
	retMat.RotationX(angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> RotationY(float angle)
{
	SquareMatrix<T, 3> retMat;
	retMat.RotationY(angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> RotationY(float angle)
{
	SquareMatrix<T, 4> retMat;
	retMat.RotationY(angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 3> RotationZ(float angle)
{
	SquareMatrix<T, 3> retMat;
	retMat.RotationZ(angle);
	return retMat;
}

template<typename T>
SquareMatrix<T, 4> RotationZ(float angle)
{
	SquareMatrix<T, 4> retMat;
	retMat.RotationZ(angle);
	return retMat;
}

template<typename T, std::size_t size>
T Determinant(const SquareMatrix<T, size>& mat)
{
	return mat.Determinant();
}

template<typename T, std::size_t size>
T Trace(const SquareMatrix<T, size>& mat)
{
	return mat.Trace();
}

template<typename T>
Vector<T, 3> TransformVec(const SquareMatrix<T, 4>& mat, const Vector<T, 3>& vec)
{
	return mat.TransformVec(vec);
}

template<typename T>
Vector<T, 3> TransformPoint(const SquareMatrix<T, 4>& mat, const Vector<T, 3>& point)
{
	return mat.TransformPoint(point);
}

// SizedMatrixOperator implementations
template<typename T>
constexpr interior::SizedMatrixOperator<T>::SizedMatrixOperator(std::size_t inRows, std::size_t inCols, T* pMem)
	: rows(inRows), cols(inCols), pData(pMem)
{}

template<typename T>
constexpr interior::SizedMatrixOperator<T>::SizedMatrixOperator(const SizedMatrixOperator<T>& op)
	: rows(op.rows), cols(op.cols), pData(op.pData)
{}

template<typename T>
void interior::SizedMatrixOperator<T>::LoopedCopyOtherRaw(const T* const other)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] = other[row * cols + col];
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::FillFromInitializerList(std::initializer_list<T> args)
{
	std::size_t passedInValsCount = std::min(rows*cols, args.size());
	std::size_t i = 0;
	for (const T& arg : args)
	{
		if (i < passedInValsCount)
		{
			pData[i] = arg;
			++i;
		}
		else
		{
			break;
		}
	}
	for (; i < rows * cols; ++i)
	{
		pData[i] = 0;
	}
}

template<typename T>
T& interior::SizedMatrixOperator<T>::operator()(std::size_t row, std::size_t col)
{
	// Add const to *this's type to call const version of operator() and then cast away const on the return
	return const_cast<T&>(static_cast<const SizedMatrixOperator<T>&>(*this)(row, col));
}

template<typename T>
const T& interior::SizedMatrixOperator<T>::operator()(std::size_t row, std::size_t col) const
{
	if (row >= rows)
	{
		throw std::out_of_range("Operator () row index out of bounds on Matrix struct");
	}
	if (col >= cols)
	{
		throw std::out_of_range("Operator () column index out of bounds on Matrix struct");
	}
	return data[row * cols + col];
}

template<typename T>
void interior::SizedMatrixOperator<T>::operator+=(const interior::SizedMatrixOperator<T>& rhs)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] += rhs.pData[row * cols + col];
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::operator-=(const interior::SizedMatrixOperator<T>& rhs)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] -= rhs.pData[row * cols + col];
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::operator*=(const interior::SizedMatrixOperator<T>& rhs)
{
	// Temporarily store results of given row dotted with each column of rhs to work in place
	T* placeholderRow = new T[rhs.cols];
	for (std::size_t col = 0; col < rhs.cols; ++col)
	{
		placeholderRow[col] = 0;
	}

	for (std::size_t row = 0; row < rows; ++row)
	{
		// For each column in rhs
		for (std::size_t rhsCol = 0; rhsCol < rhs.cols; ++rhsCol)
		{
			// Dot each column of this with the row in rhs
			for (std::size_t innerCol = 0; innerCol < cols; ++innerCol)
			{
				placeholderRow[rhsCol] += pData[row * cols + innerCol] * rhs.pData[innerCol * rhs.cols + rhsCol];
			}
		}
		for (std::size_t col = 0; col < rhs.cols; ++col)
		{
			pData[row * cols + col] = placeholderRow[col];
			placeholderRow[col] = 0;
		}
	}

	delete[] placeholderRow;
}

template<typename T>
template<typename S>
void interior::SizedMatrixOperator<T>::operator*=(const S& scalar)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] *= scalar;
		}
	}
}

template<typename T>
template<typename S>
void interior::SizedMatrixOperator<T>::operator/=(const S& scalar)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] /= scalar;
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::MatrixMultiply(const interior::SizedMatrixOperator<T>& lhs, const interior::SizedMatrixOperator<T>& rhs)
{
	for (std::size_t row = 0; row < lhs.rows; ++row)
	{
		// For each column in rhs
		for (std::size_t rhsCol = 0; rhsCol < rhs.cols; ++rhsCol)
		{
			// Dot each col of lhs with the row in rhs
			for (std::size_t innerCol = 0; innerCol < lhs.cols; ++innerCol)
			{
				pData[row * lhs.cols + rhsCol] += lhs.pData[row * lhs.cols + innerCol] * rhs.pData[innerCol * rhs.cols + rhsCol];
			}
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::Transpose(const T* const mat)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[row * cols + col] = mat[col * rows + row];
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::ColVecMult(T* retVec, const T* const vec)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			retVec[row] += pData[row * cols + col] * vec[col];
		}
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::RowVecMult(T* retVec, const T* const vec)
{
	for (std::size_t col = 0; col < cols; ++col)
	{
		for (std::size_t row = 0; row < rows; ++row)
		{
			retVec[col] += vec[row] * pData[row * cols + col];
		}
	}
}

template<typename T>
std::size_t interior::SizedMatrixOperator<T>::GetNumRows() const
{
	return rows;
}

template<typename T>
std::size_t interior::SizedMatrixOperator<T>::GetNumCols() const
{
	return cols;
}

template<typename T>
void interior::SizedMatrixOperator<T>::SetRow(std::size_t row, const T* const vec)
{
	for (std::size_t col = 0; col < cols; ++c)
	{
		data[row * cols + col] = vec[col];
	}
}

template<typename T>
void interior::SizedMatrixOperator<T>::SetCol(std::size_t col, const T* const vec)
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		data[row * cols + col] = vec[row];
	}
}

// SizedSquareMatrixOperator implementations
template<typename T>
constexpr interior::SizedSquareMatrixOperator<T>::SizedSquareMatrixOperator(std::size_t size, T* pMem)
	: SizedMatrixOperator(size, size, pMem)
{}

template<typename T>
constexpr interior::SizedSquareMatrixOperator<T>::SizedSquareMatrixOperator(const SizedMatrixOperator<T>& op)
	: SizedMatrixOperator(op)
{}

template<typename T>
void interior::SizedSquareMatrixOperator<T>::Identity()
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = 0; col < cols; ++col)
		{
			if (row == col)
			{
				pData[row * cols + col] = 1;
			}
			else
			{
				pData[row * cols + col] = 0;
			}
		}
	}
}

template<typename T>
void interior::SizedSquareMatrixOperator<T>::Transpose()
{
	for (std::size_t row = 0; row < rows; ++row)
	{
		for (std::size_t col = row + 1; col < cols; ++col)
		{
			T temp = pData[row * cols + col];
			pData[row * cols + col] = pData[col * rows + row];
			pData[col * rows + row] = temp;
		}
	}
}

template<typename T>
bool interior::SizedSquareMatrixOperator<T>::TryInvert()
{
	// Remember which row was swapped with the one at the current index to undo it at the end
	std::size_t* swappedRows = new std::size_t[rows];
	for (std::size_t row = 0; row < rows; ++row)
	{
		swappedRows[row] = row;
	}

	// Operate moving along the diagonal
	for (std::size_t pivot = 0; pivot < rows; ++pivot)
	{
		// Find the largest element in the current column for better numerical precision
		std::size_t maxRow = pivot;
		T maxElem = abs(pData[pivot * cols + pivot]);
		for (std::size_t row = pivot + 1; row < rows; ++row)
		{
			T element = abs(pData[row * cols + pivot]);
			if (element > maxElem)
			{
				maxElem = element;
				maxRow = row;
			}
		}

		// A column must be all zeroes, non-invertible
		if (Math::IsZero(maxElem))
		{
			delete[] swappedRows;
			return false;
		}

		swappedRows[pivot] = maxRow;
		// If maxElem is not in the pivot row, swap rows
		if (maxRow != pivot)
		{
			for (std::size_t col = 0; col < cols; ++col)
			{
				T temp = pData[pivot * cols + col];
				pData[pivot * cols + col] = pData[maxRow * cols + col];
				pData[maxRow * cols + col] = temp;
			}
		}

		// Scale the pivot row by 1/pivot element to set the diagonal entry to 1
		float pivotRecip = 1.0f / pData[pivot * cols + pivot];
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[pivot * cols + col] *= pivotRecip;
		}

		// Set the pivot element to its reciprocal to invert in place
		pData[pivot * cols + pivot] = pivotRecip;

		// Set all pivot column elements except pivot row's to 0 by adding multiples of pivot row
		for (std::size_t row = 0; row < rows; ++row)
		{
			if (row == pivot)
			{
				continue;
			}

			// factor is multiple with which to subtract pivot row from changing row
			T factor = pData[row * cols + pivot];
			// Set pivot column element to 0 so it ends up being -factor*pivot reciprocal, as it should be to invert in place,
			//  because the augmented identity matrix had a 0 there
			pData[row * cols + pivot] = 0;
			for (std::size_t col = 0; col < cols; ++col)
			{
				pData[row * cols + col] -= factor * pData[pivot * cols + col];
			}
		}

		// Undo swaps by column in reverse order; column because when rows swap the hidden, in-place rows don't,
		//  meaning the opposite columns had a 1 in the augmented identity matrix
		for (int p = rows - 1; p >= 0; --p)
		{
			if (swappedRows[p] != p)
			{
				for (std::size_t row = 0; row < rows; ++row)
				{
					T temp = pData[row * rows + swappedRows[p]];
					pData[row * rows + swappedRows[p]] = pData[row * rows + p];
					pData[row * rows + p] = temp;
				}
			}
		}
	}
	delete[] swappedRows;
	return true;
}

template<typename T>
T interior::SizedSquareMatrixOperator<T>::Determinant() const
{
	T runningProduct = 1;

	// Operate moving along the diagonal
	for (std::size_t pivot = 0; pivot < rows; ++pivot)
	{
		// Find the largest element in the current column for better numerical precision
		std::size_t maxRow = pivot;
		T maxElem = abs(pData[pivot * cols + pivot]);
		for (std::size_t row = pivot + 1; row < rows; ++row)
		{
			T element = abs(pData[row * cols + pivot]);
			if (element > maxElem)
			{
				maxElem = element;
				maxRow = row;
			}
		}

		// A column must be all zeroes, determinant is 0
		if (Math::IsZero(maxElem))
		{
			return 0;
		}

		// If maxElem is not in the pivot row, swap rows
		if (maxRow != pivot)
		{
			for (std::size_t col = 0; col < cols; ++col)
			{
				T temp = pData[pivot * cols + col];
				pData[pivot * cols + col] = pData[maxRow * cols + col];
				pData[maxRow * cols + col] = temp;
			}
			runningProduct *= -1;
		}

		// Scale the pivot row by 1/pivot element to set the diagonal entry to 1
		float pivotRecip = 1.0f / pData[pivot * cols + pivot];
		for (std::size_t col = 0; col < cols; ++col)
		{
			pData[pivot * cols + col] *= pivotRecip;
		}
		runningProduct /= pivotRecip;


		// Set all pivot column elements except pivot row's to 0 by adding multiples of pivot row
		for (std::size_t row = 0; row < rows; ++row)
		{
			if (row == pivot)
			{
				continue;
			}

			// factor is multiple with which to subtract pivot row from changing row
			T factor = pData[row * cols + pivot];
			for (std::size_t col = 0; col < cols; ++col)
			{
				pData[row * cols + col] -= factor * pData[pivot * cols + col];
			}
		}
	}
	return runningProduct;
}

template<typename T>
T interior::SizedSquareMatrixOperator<T>::Trace() const
{
	T sum = 0;
	for (std::size_t row = 0; row < rows; ++row)
	{
		sum += pData[row * cols + row];
	}
	return sum;
}