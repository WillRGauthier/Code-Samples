#pragma once
#include <type_traits>
#include "Vector.h"

// This library follows the convention where possible that functions are defined twice:
//  once as a member function that acts in-place, and once as a free function that returns a new, altered copy
// The structs in this library only accept arithmetic template arguments
// There are static constants for identity and zero quaternions

// There are 2 methods provided that perform spherical linear interpolation between quaternions: SlerpOrthonormalBasis and SlerpAngleWeights
// SlerpOrthonormalBasis is based on Jonathan Blow's coordinate-free derivation of slerp using an orthonormal basis and polar coordinates:
//  -http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/
// SlerpAngleWeights is based on the usual Shoemake formula where the weights of each quaternion are interpolating functions involving sines of the angle between them
// Users should profile which implementation is faster, although lerp should generally be sufficient and efficient

template<typename T>
struct Quaternion
{
	static_assert(std::is_arithmetic<T>::value, "Quaternion only accepts arithmetic template arguments");

	T x, y, z, w;

	// Default to identity quaternion
	constexpr Quaternion();
	constexpr Quaternion(T inX, T inY, T inZ, T inW);
	// Create rotating quaternion about axis by angle radians (normalizes axis)
	Quaternion(const Vector<T, 3>& axis, float angle);

	// Set this quaternion's internal values directly
	Quaternion<T>& Set(T inX, T inY, T inZ, T inW);
	// Change this quaternion to rotation about axis by angle radians (normalizes axis)
	Quaternion<T>& Set(const Vector<T, 3>& axis, float angle);

	// Component-wise quaternion addition
	Quaternion<T> operator+(const Quaternion<T>& rhs) const;
	// Component-wise quaternion +=
	Quaternion<T>& operator+=(const Quaternion<T>& rhs);
	// Component-wise quaternion subtraction
	Quaternion<T> operator-(const Quaternion<T>& rhs) const;
	// Component-wise quaternion -=
	Quaternion<T>& operator-=(const Quaternion<T>& rhs);
	// Negate unary -
	Quaternion<T> operator-() const;
	// Scalar *=
	Quaternion<T>& operator*=(T scalar);
	// Quaternion multiplication/concatenation
	Quaternion<T> operator*(const Quaternion<T>& rhs) const;
	// Quaternion multiplication/concatenation
	Quaternion<T>& operator*=(const Quaternion<T>& rhs);

	// Length squared of quaternion
	T LengthSq() const;
	// Length of quaternion
	T Length() const;

	// Normalize this quaternion in place
	Quaternion<T>& Normalize();
	// Change this quaternion into a zero quaternion
	Quaternion<T>& Zero();
	// Change this quaternion into an identity quaternion
	Quaternion<T>& Identity();

	// Conjugate is equivalent to Inverse without normalization step
	Quaternion<T>& Conjugate();
	Quaternion<T>& Inverse();

	// Dot product
	T Dot(const Quaternion<T>& rhs) const;

	// Vector rotation assuming unit quaternion
	Vector<T, 3> Transform(const Vector<T, 3>& vec) const;

	// Linear interpolation where this quaternion is starting rotation assuming unit quaternions; normalizes returned quaternion
	Quaternion<T> Lerp(const Quaternion<T>& end, float t) const;
	// Spherical linear interpolation where this quaternion is starting rotation assuming unit quaternions; does not normalize returned quaternion
	// (see top of file for more details)
	Quaternion<T> SlerpOrthonormalBasis(const Quaternion<T>& end, float t) const;
	// Spherical linear interpolation where this quaternion is starting rotation assuming unit quaternions; does not normalize returned quaternion
	// (see top of file for more details)
	Quaternion<T> SlerpAngleWeights(const Quaternion<T>& end, float t) const;

	// Useful defaults
	static const Quaternion<T> zero;
	static const Quaternion<T> identity;
};

// Quaternion free functions
// Scalar multiplication
template<typename T>
Quaternion<T> operator*(T scalar, const Quaternion<T>& quat);

// Length squared of quat
template<typename T>
T LengthSq(const Quaternion<T>& quat);
// Length of quat
template<typename T>
T Length(const Quaternion<T>& quat);

// Return normalized copy of quat
template<typename T>
Quaternion<T> Normalize(const Quaternion<T>& quat);
// Return zero quaternion; Caller may have to assist compiler with type deduction by specifying type in angle brackets(i.e.Zero<int>())
template<typename T>
Quaternion<T> Zero();
// Return identity quaternion; Caller may have to assist compiler with type deduction by specifying type in angle brackets(i.e.Identity<float>())
template<typename T>
Quaternion<T> Identity();

// Conjugate is equivalent to Inverse without normalization step
template<typename T>
Quaternion<T> Conjugate(const Quaternion<T>& quat);
template<typename T>
Quaternion<T> Inverse(const Quaternion<T>& quat);

// Dot product
template<typename T>
T Dot(const Quaternion<T>& lhs, const Quaternion<T>& rhs);

// Vector rotation assuming quat is unit
template<typename T>
Vector<T, 3> Transform(const Quaternion<T>& quat, const Vector<T, 3>& vec);

// Linear interpolation assuming unit quaternions; normalizes returned quaternion
template<typename T>
Quaternion<T> Lerp(const Quaternion<T>& start, const Quaternion<T>& end, float t);
// Spherical linear interpolation where this quaternion is starting rotation assuming unit quaternions; does not normalize returned quaternion
// (see top of file for more details)
template<typename T>
Quaternion<T> SlerpOrthonormalBasis(const Quaternion<T>& start, const Quaternion<T>& end, float t);
// Spherical linear interpolation where this quaternion is starting rotation assuming unit quaternions; does not normalize returned quaternion
// (see top of file for more details)
template<typename T>
Quaternion<T> SlerpAngleWeights(const Quaternion<T>& start, const Quaternion<T>& end, float t);

// Implementations
// Quaternion member implementations
template<typename T>
constexpr Quaternion<T>::Quaternion()
	: x(0), y(0), z(0), w(1)
{}

template<typename T>
constexpr Quaternion<T>::Quaternion(T inX, T inY, T inZ, T inW)
	: x(inX), y(inY), z(inZ), w(inW)
{}

template<typename T>
Quaternion<T>::Quaternion(const Vector<T, 3>& axis, float angle)
{
	Set(axis, angle);
}

template<typename T>
Quaternion<T>& Quaternion<T>::Set(T inX, T inY, T inZ, T inW)
{
	x = inX;
	y = inY;
	z = inZ;
	w = inW;
	return *this;
}

template<typename T>
Quaternion<T>& Quaternion<T>::Set(const Vector<T, 3>& axis, float angle)
{
	Vector<T, 3> normalizedAxis = ::Normalize(axis);
	T halfSin = sin(angle / 2.0f);
	x = normalizedAxis.data[0] * halfSin;
	y = normalizedAxis.data[1] * halfSin;
	z = normalizedAxis.data[2] * halfSin;
	w = cos(angle / 2.0f);
	return *this;
}

template<typename T>
Quaternion<T> Quaternion<T>::operator+(const Quaternion<T>& rhs) const
{
	return Quaternion<T> (x + rhs.x, y + rhs.y, z + rhs.z, w + rhs.w);
}

template<typename T>
Quaternion<T>& Quaternion<T>::operator+=(const Quaternion<T>& rhs)
{
	x += rhs.x;
	y += rhs.y;
	z += rhs.z;
	w += rhs.w;
	return *this;
}

template<typename T>
Quaternion<T> Quaternion<T>::operator-(const Quaternion<T>& rhs) const
{
	return Quaternion<T> (x - rhs.x, y - rhs.y, z - rhs.z, w - rhs.w);\
}

template<typename T>
Quaternion<T>& Quaternion<T>::operator-=(const Quaternion<T>& rhs)
{
	x -= rhs.x;
	y -= rhs.y;
	z -= rhs.z;
	w -= rhs.w;
	return *thsi;
}

template<typename T>
Quaternion<T> Quaternion<T>::operator-() const
{
	return Quaternion<T> (-x, -y, -z, -w);
}

template<typename T>
Quaternion<T>& Quaternion<T>::operator*=(T scalar)
{
	x *= scalar;
	y *= scalar;
	z *= scalar;
	w *= scalar;
	return *this;
}

template<typename T>
Quaternion<T> Quaternion<T>::operator*(const Quaternion<T>& rhs) const
{
	return Quaternion<T>(w * rhs.x + rhs.w * x + y * rhs.z - z * rhs.y,
						 w * rhs.y + rhs.w * y + z * rhs.x - x * rhs.z,
						 w * rhs.z + rhs.w * z + x * rhs.y - y * rhs.x,
						 w * rhs.w - x * rhs.x - y * rhs.y - z * rhs.z);
}

template<typename T>
Quaternion<T>& Quaternion<T>::operator*=(const Quaternion<T>& rhs)
{
	x = w * rhs.x + rhs.w * x + y * rhs.z - z * rhs.y;
	y = w * rhs.y + rhs.w * y + z * rhs.x - x * rhs.z;
	z = w * rhs.z + rhs.w * z + x * rhs.y - y * rhs.x;
	w = w * rhs.w - x * rhs.x - y * rhs.y - z * rhs.z;
	return *this;
}

template<typename T>
T Quaternion<T>::LengthSq() const
{
	return x * x + y * y + z * z + w * w;
}

template<typename T>
T Quaternion<T>::Length() const
{
	return sqrt(x * x + y * y + z * z + w * w);
}

template<typename T>
Quaternion<T>& Quaternion<T>::Normalize()
{
	T length = Length();
	x /= length;
	y /= length;
	z /= length;
	w /= length;
	return *this;
}

template<typename T>
Quaternion<T>& Quaternion<T>::Zero()
{
	x = y = z = w = 0;
	return *this;
}

template<typename T>
Quaternion<T>& Quaternion<T>::Identity()
{
	x = y = z = 0;
	w = 1;
	return *this;
}

template<typename T>
Quaternion<T>& Quaternion<T>::Conjugate()
{
	x *= -1;
	y *= -1;
	z *= -1;
	return *this;
}

template<typename T>
Quaternion<T>& Quaternion<T>::Inverse()
{
	T lengthSqRecip = 1.0f / (x * x + y * y + z * z + w * w);
	x *= -lengthSqRecip;
	y *= -lengthSqRecip;
	z *= -lengthSqRecip;
	w *= lengthSqRecip;
	return *this;
}

template<typename T>
T Quaternion<T>::Dot(const Quaternion<T>& rhs) const
{
	return x * rhs.x + y * rhs.y + z * rhs.z + w * rhs.w;
}

template<typename T>
Vector<T, 3> Quaternion<T>::Transform(const Vector<T, 3>& vec) const
{
	T crossMultCoefficient = 2 * w;
	T vectorCoefficient = 2 * crossMultCoefficient - 1;
	T quatCoefficient = 2 * (x * vec.data[0] + y * vec.data[1] + z * vec.data[2]);
	return Vector<T, 3> (vectorCoefficient * vec.data[0] + quatCoefficient * x + crossMultCoefficient * (y * vec.data[2] - z * vec.data[1]),
						 vectorCoefficient * vec.data[1] + quatCoefficient * y + crossMultCoefficient * (z * vec.data[0] - x * vec.data[2]),
						 vectorCoefficient * vec.data[2] + quatCoefficient * z + crossMultCoefficient * (x * vec.data[1] - y * vec.data[0]));
}

template<typename T>
Quaternion<T> Quaternion<T>::Lerp(const Quaternion<T>& end, float t) const
{
	Quaternion<T> retQuat;
	T dot = x * end.x + y * end.y + z * end.z + w * end.w;
	// If dot product (cosine between the quaternions) is negative, angle between them is greater than 90 degrees, so lerp will take longer arc along the sphere
	// Avoid by negating one of the quaternions
	if (Math::IsZero(dot + abs(dot)))
	{
		retQuat.x = -x + t * (end.x + x);
		retQuat.y = -y + t * (end.y + y);
		retQuat.z = -z + t * (end.z + z);
		retQuat.w = -w + t * (end.w + w);
	}
	else
	{
		retQuat.x = x + t * (end.x - x);
		retQuat.y = y + t * (end.y - y);
		retQuat.z = z + t * (end.z - z);
		retQuat.w = w + t * (end.w - w);
	}
	retQuat.Normalize();
	return retQuat;
}

template<typename T>
Quaternion<T> Quaternion<T>::SlerpOrthonormalBasis(const Quaternion<T>& end, float t) const
{
	Quaternion<T> maybeNegStart(*this);

	T dot = x * end.x + y * end.y + z * end.z + w * end.w;
	// If dot product (cosine between the quaternions) is negative, angle between them is greater than 90 degrees, so lerp will take longer arc along the sphere
	// Avoid by negating one of the quaternions
	if (Math::IsZero(dot + abs(dot)))
	{
		maybeNegStart *= -1;
		dot = -dot;
	}

	// If quaternions are close to 'collinear', use lerp
	if (dot > 0.9995f)
	{
		return Lerp(end, t);
	}
	else
	{
		T thetaWhole = acos(dot);
		T thetaDesired = t * thetaWhole;
		// Use Gram-Schmidt Orthogonalization to create a quaternion orthogonal to start
		Quaternion<T> basisQuat = end - maybeNegStart * dot;
		// Normalize to get an orthonormal basis on the unit hypersphere
		basisQuat.Normalize();
		// Use polar coordinates to find the quaternion with angle thetaDesired
		return maybeNegStart * cos(thetaDesired) + basisQuat * sin(thetaDesired);
	}	
}

template<typename T>
Quaternion<T> Quaternion<T>::SlerpAngleWeights(const Quaternion<T>& end, float t) const
{
	Quaternion<T> maybeNegStart(*this);

	T dot = x * end.x + y * end.y + z * end.z + w * end.w;
	// If dot product (cosine between the quaternions) is negative, angle between them is greater than 90 degrees, so lerp will take longer arc along the sphere
	// Avoid by negating one of the quaternions
	if (Math::IsZero(dot + abs(dot)))
	{
		maybeNegStart *= -1;
		dot = -dot;
	}

	// If quaternions are close to 'collinear', use lerp
	if (dot > 0.9995f)
	{
		return Lerp(end, t);
	}
	else
	{
		T theta = acos(dot);
		T sinThetaRecip = 1 / sin(theta);
		T startRatio = sin((1 - t) * theta) * sinThetaRecip;
		T endRatio = sin(t * theta) * sinThetaRecip;
		return *this * startRatio + end * endRatio;
	}
}

template<typename T>
const Quaternion<T> Quaternion<T>::zero(0, 0, 0, 0);
template<typename T>
const Quaternion<T> Quaternion<T>::identity(0, 0, 0, 1);

// Quaternion free function implementations
template<typename T>
Quaternion<T> operator*(T scalar, const Quaternion<T>& quat)
{
	return Quaternion<T> (quat.x * scalar, quat.y * scalar, quat.z * scalar, quat.w * scalar);
}

template<typename T>
T LengthSq(const Quaternion<T>& quat)
{
	return quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w;
}

template<typename T>
T Length(const Quaternion<T>& quat)
{
	return sqrt(quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);
}

template<typename T>
Quaternion<T> Normalize(const Quaternion<T>& quat)
{
	Quaternion<T> retQuat(quat);
	retQuat.Normalize();
	return retQuat;
}

template<typename T>
Quaternion<T> Zero()
{
	return Quaternion<T>::zero;
}

template<typename T>
Quaternion<T> Identity()
{
	return Quaternion<T>::identity;
}

template<typename T>
Quaternion<T> Conjugate(const Quaternion<T>& quat)
{
	return Quaternion<T> retQuat(-quat.x, -quat.y, -quat.z, quat.w);
}

template<typename T>
Quaternion<T> Inverse(const Quaternion<T>& quat)
{
	Quaternion<T> retQuat(quat);
	retQuat.Inverse();
	return retQuat;
}

template<typename T>
T Dot(const Quaternion<T>& lhs, const Quaternion<T>& rhs)
{
	return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
}

template<typename T>
Vector<T, 3> Transform(const Quaternion<T>& quat, const Vector<T, 3>& vec)
{
	return quat.Transform(vec);
}

template<typename T>
Quaternion<T> Lerp(const Quaternion<T>& start, const Quaternion<T>& end, float t)
{
	return start.Lerp(end, t);
}

template<typename T>
Quaternion<T> SlerpOrthonormalBasis(const Quaternion<T>& start, const Quaternion<T>& end, float t)
{
	return start.SlerpOrthonormalBasis(end, t);
}

template<typename T>
Quaternion<T> SlerpAngleWeights(const Quaternion<T>& start, const Quaternion<T>& end, float t)
{
	return start.SlerpAngleWeights(end, t);
}