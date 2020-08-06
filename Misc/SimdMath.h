#pragma once
#include "Math.h"
#include <xmmintrin.h>
#include <smmintrin.h>

// SHUFFLER is like shuffle, but has easier to understand indices
#define _MM_SHUFFLER( xi, yi, zi, wi ) _MM_SHUFFLE( wi, zi, yi, xi )

class alignas(16) SimdVector3
{
	// Underlying vector
	__m128 mVec;
public:
	// Empty default constructor
	SimdVector3() { }

	// Constructor from __m128
	explicit SimdVector3(__m128 vec)
	{
		mVec = vec;
	}

	// Constructor if converting from Vector3
	explicit SimdVector3(const Vector3& vec)
	{
		FromVector3(vec);
	}

	// Load from a Vector3 into this SimdVector3
	void FromVector3(const Vector3& vec)
	{
		// We can't assume this is aligned
		mVec = _mm_setr_ps(vec.x, vec.y, vec.z, 0.0f);
	}

	// Convert this SimdVector3 to a Vector3
	Vector3 ToVector3() const
	{
		return Vector3(mVec);
	}

	// this = this + other
	void Add(const SimdVector3& other)
	{
		mVec = _mm_add_ps(mVec, other.mVec);
	}

	// this = this - other
	void Sub(const SimdVector3& other)
	{
		mVec = _mm_sub_ps(mVec, other.mVec);
	}

	// this = this * other (componentwise)
	void Mul(const SimdVector3& other)
	{
		mVec = _mm_mul_ps(mVec, other.mVec);
	}

	// this = this * scalar
	void Mul(float scalar)
	{
		// Create an __m128 to hold the scalar in all its components
		__m128 scalarVec = _mm_set_ps1(scalar);
		// Multiply all components by the scalar
		mVec = _mm_mul_ps(mVec, scalarVec);
	}

	// Normalize this vector
	void Normalize()
	{
		// Calculate lenght squared (data dot data)
		// The mask 0x77 will dot the x, y, and z components and store it in the result's x, y, and z
		__m128 temp = _mm_dp_ps(mVec, mVec, 0x77);
		// Store 1/length in x, y, and z
		temp = _mm_rsqrt_ps(temp);
		// Multiply all components by 1/length
		mVec = _mm_mul_ps(mVec, temp);
	}

	// (this dot other), storing the dot product
	// in EVERY COMPONENT of returned SimdVector3
	SimdVector3 Dot(const SimdVector3& other) const
	{
		__m128 temp = _mm_dp_ps(mVec, other.mVec, 0x7F);
		return SimdVector3(temp);
	}

	// Length Squared of this, storing the result in
	// EVERY COMPONENT of returned SimdVector3
	SimdVector3 LengthSq() const
	{
		__m128 temp = _mm_dp_ps(mVec, mVec, 0x7F);
		return SimdVector3(temp);
	}

	// Length of this, storing the result in
	// EVERY COMPONENT of returned SimdVector3
	SimdVector3 Length() const
	{
		__m128 temp = _mm_dp_ps(mVec, mVec, 0x7F);
		temp = _mm_sqrt_ps(temp);
		return SimdVector3(temp);
	}

	// result = this (cross) other
	SimdVector3 Cross(const SimdVector3& other) const
	{
		// Vectorized formula: (<Ay,Az,Ax>*<Bz,Bx,By>)-(<Az,Ax,Ay>*<By,Bz,Bx>)
		// Shuffle A to <Ay,Az,Ax>
		__m128 tempA = _mm_shuffle_ps(mVec, mVec, _MM_SHUFFLER(1, 2, 0, 3));
		// Shuffle B to <Bz,Bx,By>
		__m128 tempB = _mm_shuffle_ps(other.mVec, other.mVec, _MM_SHUFFLER(2, 0, 1, 3));
		// result = tempA*tempB
		__m128 result = _mm_mul_ps(tempA, tempB);
		// Shuffle A to <Az,Ax,Ay>
		tempA = _mm_shuffle_ps(mVec, mVec, _MM_SHUFFLER(2, 0, 1, 3));
		// Shuffle B to <By,Bz,Bx>
		tempB = _mm_shuffle_ps(other.mVec, other.mVec, _MM_SHUFFLER(1, 2, 0, 3));
		// tempA = tempA*tempB
		tempA = _mm_mul_ps(tempA, tempB);
		// result = result - tempA
		result = _mm_sub_ps(result, tempA);
		return SimdVector3(result);
	}

	// result = this * (1.0f - f) + other * f
	SimdVector3 Lerp(const SimdVector3& other, float f) const
	{
		// this * (1.0f - f)
		__m128 thisTemp = _mm_set_ps1(1.0f - f);
		thisTemp = _mm_mul_ps(mVec, thisTemp);
		// other * F
		__m128 otherTemp = _mm_set_ps1(f);
		otherTemp = _mm_mul_ps(other.mVec, otherTemp);

		thisTemp = _mm_add_ps(thisTemp, otherTemp);
		return SimdVector3(thisTemp);
	}

	friend SimdVector3 Transform(const SimdVector3& vec, const class SimdMatrix4& mat, float w);
};

class alignas(16) SimdMatrix4
{
	// Four vectors, one for each row
	__m128 mRows[4];
public:
	// Empty default constructor
	SimdMatrix4() { }

	// Constructor from array of four __m128s
	explicit SimdMatrix4(__m128 rows[4])
	{
		memcpy(mRows, rows, sizeof(__m128) * 4);
	}

	// Constructor if converting from Matrix4
	explicit SimdMatrix4(const Matrix4& mat)
	{
		FromMatrix4(mat);
	}

	// Load from a Matrix4 into this SimdMatrix4
	void FromMatrix4(const Matrix4& mat)
	{
		// We can't assume that mat is aligned, so
		// we can't use mm_set_ps
		memcpy(mRows, mat.mat, sizeof(float) * 16);
	}

	// Convert this SimdMatrix4 to a Matrix4
	Matrix4 ToMatrix4()
	{
		return Matrix4(mRows);
	}

	// this = this * other
	void Mul(const SimdMatrix4& other)
	{
		// SimdMatrix4 uses one __m128 per row not column, so the matrix must be transposed
		// _MM_TRANSPOSE4_PS macro is destructive, so copies must be made
		__m128 rowCopies[4] = {other.mRows[0], other.mRows[1], other.mRows[2], other.mRows[3]};
		_MM_TRANSPOSE4_PS(rowCopies[0], rowCopies[1], rowCopies[2], rowCopies[3]);

		__m128 tempRows[4];
		for (int i = 0; i < 4; ++i)
		{
			// Multiply a given row with all the original columns of other, using masking to store the result in a single element
			//  i.e. <C_x#,0,0,0>, <0,C_y#,0,0>, <0,0,C_z#,0>, <0,0,0,C_w#>
			// C_x# = A.row# (dot) B^T.row0
			tempRows[0] = _mm_dp_ps(mRows[i], rowCopies[0], 0xF1);
			// C_y# = A.row# (dot) B^T.row1
			tempRows[1] = _mm_dp_ps(mRows[i], rowCopies[1], 0xF2);
			// C_z# = A.row# (dot) B^T.row2
			tempRows[2] = _mm_dp_ps(mRows[i], rowCopies[2], 0xF4);
			// C_w# = A.row# (dot) B^T.row3
			tempRows[3] = _mm_dp_ps(mRows[i], rowCopies[3], 0XF8);
			// Final vector constructed with a series of additions
			tempRows[0] = _mm_add_ps(tempRows[0], tempRows[1]);
			tempRows[0] = _mm_add_ps(tempRows[0], tempRows[2]);
			tempRows[0] = _mm_add_ps(tempRows[0], tempRows[3]);
			// Save the calculated row in this
			mRows[i] = tempRows[0];
		}
	}

	// Transpose this matrix
	void Transpose()
	{
		_MM_TRANSPOSE4_PS(mRows[0], mRows[1], mRows[2], mRows[3]);
	}

	// Loads a Scale matrix into this
	void LoadScale(float scale)
	{
		// scale 0 0 0
		mRows[0] = _mm_set_ss(scale);
		mRows[0] = _mm_shuffle_ps(mRows[0], mRows[0], _MM_SHUFFLE(1, 1, 1, 0));

		// 0 scale 0 0
		mRows[1] = _mm_set_ss(scale);
		mRows[1] = _mm_shuffle_ps(mRows[1], mRows[1], _MM_SHUFFLE(1, 1, 0, 1));

		// 0 0 scale 0
		mRows[2] = _mm_set_ss(scale);
		mRows[2] = _mm_shuffle_ps(mRows[2], mRows[2], _MM_SHUFFLE(1, 0, 1, 1));

		// 0 0 0 1
		mRows[3] = _mm_set_ss(1.0f);
		mRows[3] = _mm_shuffle_ps(mRows[3], mRows[3], _MM_SHUFFLE(0, 1, 1, 1));
	}

	// Loads a rotation about the X axis into this
	void LoadRotationX(float angle)
	{
		// 1 0 0 0
		mRows[0] = _mm_set_ss(1.0f);
		mRows[0] = _mm_shuffle_ps(mRows[0], mRows[0], _MM_SHUFFLE(1, 1, 1, 0));

		float cosTheta = Math::Cos(angle);
		float sinTheta = Math::Sin(angle);

		// 0 cos sin 0
		mRows[1] = _mm_setr_ps(0.0f, cosTheta, sinTheta, 0.0f);

		// 0 -sin cos 0
		mRows[2] = _mm_setr_ps(0.0f, -sinTheta, cosTheta, 0.0f);

		// 0 0 0 1
		mRows[3] = _mm_set_ss(1.0f);
		mRows[3] = _mm_shuffle_ps(mRows[3], mRows[3], _MM_SHUFFLE(0, 1, 1, 1));
	}

	// Loads a rotation about the Y axis into this
	void LoadRotationY(float angle)
	{
		float cosTheta = Math::Cos(angle);
		float sinTheta = Math::Sin(angle);

		// cos 0 -sin 0
		mRows[0] = _mm_setr_ps(cosTheta, 0.0f, -sinTheta, 0.0f);

		// 0 1 0 0
		mRows[1] = _mm_set_ss(1.0f);
		mRows[1] = _mm_shuffle_ps(mRows[1], mRows[1], _MM_SHUFFLER(1, 0, 1, 1));

		// sin 0 cos 0
		mRows[2] = _mm_setr_ps(sinTheta, 0.0f, cosTheta, 0.0f);

		// 0 0 0 1
		mRows[3] = _mm_set_ss(1.0f);
		mRows[3] = _mm_shuffle_ps(mRows[3], mRows[3], _MM_SHUFFLE(0, 1, 1, 1));
	}

	// Loads a rotation about the Z axis into this
	void LoadRotationZ(float angle)
	{
		float cosTheta = Math::Cos(angle);
		float sinTheta = Math::Sin(angle);

		// cos sin 0 0
		mRows[0] = _mm_setr_ps(cosTheta, sinTheta, 0.0f, 0.0f);

		// -sin cos 0 0
		mRows[1] = _mm_setr_ps(-sinTheta, cosTheta, 0.0f, 0.0f);

		// 0 0 1 0
		mRows[2] = _mm_set_ss(1.0f);
		mRows[2] = _mm_shuffle_ps(mRows[2], mRows[2], _MM_SHUFFLER(1, 1, 0, 1));

		// 0 0 0 1
		mRows[3] = _mm_set_ss(1.0f);
		mRows[3] = _mm_shuffle_ps(mRows[3], mRows[3], _MM_SHUFFLE(0, 1, 1, 1));
	}

	// Loads a translation matrix into this
	void LoadTranslation(const Vector3& trans)
	{
		// 1 0 0 0
		mRows[0] = _mm_set_ss(1.0f);
		mRows[0] = _mm_shuffle_ps(mRows[0], mRows[0], _MM_SHUFFLER(0, 1, 1, 1));

		// 0 1 0 0
		mRows[1] = _mm_set_ss(1.0f);
		mRows[1] = _mm_shuffle_ps(mRows[1], mRows[1], _MM_SHUFFLER(1, 0, 1, 1));

		// 0 0 1 0
		mRows[2] = _mm_set_ss(1.0f);
		mRows[2] = _mm_shuffle_ps(mRows[2], mRows[2], _MM_SHUFFLER(1, 1, 0, 1));

		mRows[3] = _mm_setr_ps(trans.x, trans.y, trans.z, 1.0f);
	}

	// Loads a matrix from a quaternion into this
	void LoadFromQuaternion(const Quaternion& quat);

	// Inverts this matrix
	void Invert();

	friend SimdVector3 Transform(const SimdVector3& vec, const class SimdMatrix4& mat, float w);
};

inline SimdVector3 Transform(const SimdVector3& vec, const SimdMatrix4& mat, float w = 1.0f)
{
	// Set the w-component of the SimdVector3 to the passed in value
	__m128 temp = _mm_set_ps1(w);
	temp = _mm_insert_ps(vec.mVec, temp, 0xF0);
	// SimdMatrix4 uses one __m128 per row not column, so the matrix must be transposed
	// _MM_TRANSPOSE4_PS macro is destructive, so copies must be made
	__m128 rowCopies[4] = {mat.mRows[0], mat.mRows[1], mat.mRows[2], mat.mRows[3]};
	_MM_TRANSPOSE4_PS(rowCopies[0], rowCopies[1], rowCopies[2], rowCopies[3]);
	// Multiply the SimdVector3 with each of the original columns, using masking to store the result in a single element
	//  i.e. <x',0,0,0>, <0,y',0,0>, <0,0,z',0>, <0,0,0,w'>
	rowCopies[0] = _mm_dp_ps(temp, rowCopies[0], 0xF1);
	rowCopies[1] = _mm_dp_ps(temp, rowCopies[1], 0xF2);
	rowCopies[2] = _mm_dp_ps(temp, rowCopies[2], 0xF4);
	rowCopies[3] = _mm_dp_ps(temp, rowCopies[3], 0xF8);
	// Final vector constructed with a series of additions
	rowCopies[0] = _mm_add_ps(rowCopies[0], rowCopies[1]);
	rowCopies[0] = _mm_add_ps(rowCopies[0], rowCopies[2]);
	rowCopies[0] = _mm_add_ps(rowCopies[0], rowCopies[3]);
	return SimdVector3(rowCopies[0]);
}
