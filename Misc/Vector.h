#pragma once
#include <array>
#include <stdexcept>
#include <cmath>

// This library follows the convention where possible that functions are defined twice:
//  once as a member function that acts in-place, and once as a free function that returns a new, altered copy
// The structs in this library only accept arithmetic template arguments
// Vectors' internal data is accessible as a public std::array called data
// There are using aliases for common vector types and sizes at the end of the declarations
// Vectors sized 2, 3, and 4 have static constants of commonly useful defaults

// Turn on this #define to use anonymous structs/unions to get access to components with subscript notation (i.e. Vector2.x, Vector3.r)
// Note that it is undefined behavior, but "many compilers implement, as a non-standard language extension, the ability to read inactive members of a union"
//  -http://en.cppreference.com/w/cpp/language/union
//#define USE_NONSTANDARD_ALIAS

// Forward declare interior SizedVectorBase so it is seen as little as possible
// Note: For convenience, reproduced here is the public interface SizedVectorBase bestows on its children through inheritance
//  T& operator[](std::size_t index);
//  const T& operator[](std::size_t index) const;
//  SizedVectorBase<T>& operator*=(const S& scalar);
//  SizedVectorBase<T>& operator/=(const S& scalar);
//  SizedVectorBase<T>& Zero();
//  T LengthSq() const;
//  T Length() const;
//  void Normalize();
//  T Dot(const SizedVectorBase<T>& other) const;
//  void Saturate();
//  void Clamp(const T& min, const T& max);
//  void Abs();
namespace interior
{
	template<typename T>
	struct SizedVectorBase;
}

// Generic vector
template<typename T, std::size_t n>
struct Vector : public interior::SizedVectorBase<T>
{
	std::array<T, n> data;

	// Default to a Zero Vector
	constexpr Vector();
	constexpr explicit Vector(const T& fillVal);
	// initializer_list constructor fills missing arguments with 0 if there are fewer than n values and ignores extra values
	constexpr Vector(std::initializer_list<T> args);
	constexpr explicit Vector(const T* const rawOtherVec);
	constexpr Vector(const Vector<T, n>& other);

	constexpr Vector<T, n>& operator=(const Vector<T, n>& other);

	// Component-wise vector +=
	Vector<T, n>& operator+=(const Vector<T, n>& rhs);
	// Component-wise vector -=
	Vector<T, n>& operator-=(const Vector<T, n>& rhs);
	// Component-wise vector *=
	Vector<T, n>& operator*=(const Vector<T, n>& rhs);
	// Component-wise vector /=
	Vector<T, n>& operator/=(const Vector<T, n>& rhs);
};

// Vector2 template specialization
template<typename T>
struct Vector<T, 2> : public interior::SizedVectorBase<T>
{
#ifdef USE_NONSTANDARD_ALIAS
	union
	{
		std::array<T, 2> data;
		struct { T x, y; };
	};
#else
	std::array<T, 2> data;
#endif // USE_NONSTANDARD_ALIAS

	// Default to a Zero Vector
	constexpr Vector();
	constexpr explicit Vector(const T& fillVal);
	constexpr Vector(const T& inX, const T& inY);
	constexpr explicit Vector(const T* const rawOtherVec);
	constexpr Vector(const Vector<T, 2>& other);

	constexpr Vector<T, 2>& operator=(const Vector<T, 2>& other);

	// Component-wise vector +=
	Vector<T, 2>& operator+=(const Vector<T,2>& rhs);
	// Component-wise vector -=
	Vector<T, 2>& operator-=(const Vector<T, 2>& rhs);
	// Component-wise vector *=
	Vector<T, 2>& operator*=(const Vector<T, 2>& rhs);
	// Component-wise vector /=
	Vector<T, 2>& operator/=(const Vector<T, 2>& rhs);

	// Useful defaults
	static const Vector<T, 2> zero;
	static const Vector<T, 2> unitX;
	static const Vector<T, 2> unitY;
	static const Vector<T, 2> negUnitX;
	static const Vector<T, 2> negUnitY;
};

// Vector3 template specialization
template<typename T>
struct Vector<T, 3> : public interior::SizedVectorBase<T>
{
#ifdef USE_NONSTANDARD_ALIAS
	union
	{
		std::array<T, 3> data;
		struct { T x, y, z; };
		struct { T r, g, b; };
	};
#else
	std::array<T, 3> data;
#endif // USE_NONSTANDARD_ALIAS

	// Default to a Zero Vector
	constexpr Vector();
	constexpr explicit Vector(const T& fillVal);
	constexpr Vector(const T& inX, const T& inY, const T& inZ);
	constexpr explicit Vector(const T* const rawOtherVec);
	constexpr Vector(const Vector<T, 3>& other);

	constexpr Vector<T, 3>& operator=(const Vector<T, 3>& other);

	Vector<T, 3> Cross(const Vector<T, 3>& other) const;

	// Component-wise vector +=
	Vector<T, 3>& operator+=(const Vector<T, 3>& rhs);
	// Component-wise vector -=
	Vector<T, 3>& operator-=(const Vector<T, 3>& rhs);
	// Component-wise vector *=
	Vector<T, 3>& operator*=(const Vector<T, 3>& rhs);
	// Component-wise vector /=
	Vector<T, 3>& operator/=(const Vector<T, 3>& rhs);

	// Useful defaults
	static const Vector<T, 3> zero;
	static const Vector<T, 3> unitX;
	static const Vector<T, 3> unitY;
	static const Vector<T, 3> unitZ;
	static const Vector<T, 3> negUnitX;
	static const Vector<T, 3> negUnitY;
	static const Vector<T, 3> negUnitZ;
};

// Vector4 template specialization
template<typename T>
struct Vector<T, 4> : public interior::SizedVectorBase<T>
{
#ifdef USE_NONSTANDARD_ALIAS
	union
	{
		std::array<T, 4> data;
		struct { T x, y, z, w; };
		struct { T r, g, b, a; };
	};
#else
	std::array<T, 4> data;
#endif // USE_NONSTANDARD_ALIAS

	// Default to a Zero Vector
	constexpr Vector();
	constexpr explicit Vector(const T& fillVal);
	constexpr Vector(const T& inX, const T& inY, const T& inZ, const T& inW);
	constexpr Vector(const Vector<T, 3>& vec3, const T& scalar);
	constexpr explicit Vector(const T* const rawOtherVec);
	constexpr Vector(const Vector<T, 4>& other);

	constexpr Vector<T, 4>& operator=(const Vector<T, 4>& other);

	// Component-wise vector +=
	Vector<T, 4>& operator+=(const Vector<T, 4>& rhs);
	// Component-wise vector -=
	Vector<T, 4>& operator-=(const Vector<T, 4>& rhs);
	// Component-wise vector *=
	Vector<T, 4>& operator*=(const Vector<T, 4>& rhs);
	// Component-wise vector /=
	Vector<T, 4>& operator/=(const Vector<T, 4>& rhs);

	// Useful defaults
	static const Vector<T, 4> zero;
	static const Vector<T, 4> unitX;
	static const Vector<T, 4> unitY;
	static const Vector<T, 4> unitZ;
	static const Vector<T, 4> unitW;
	static const Vector<T, 4> negUnitX;
	static const Vector<T, 4> negUnitY;
	static const Vector<T, 4> negUnitZ;
	static const Vector<T, 4> negUnitW;
};

// Vector free functions
// Component-wise vector addition
template<typename T, std::size_t n>
Vector<T, n> operator+(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Component-wise vector subtraction
template<typename T, std::size_t n>
Vector<T, n> operator-(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Component-wise vector multiplication
template<typename T, std::size_t n>
Vector<T, n> operator*(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Component-wise vector division
template<typename T, std::size_t n>
Vector<T, n> operator/(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Scalar multiplication
template<typename T, std::size_t n, typename S>
Vector<T, n> operator* (const Vector<T, n>& vec, const S& scalar);
// Scalar multiplication
template<typename T, std::size_t n, typename S>
Vector<T, n> operator* (const S& scalar, const Vector<T, n>& vec);
// Scalar division
template<typename T, std::size_t n, typename S>
Vector<T, n> operator/(const Vector<T, n>& vec, const S& scalar);
// Negate unary -
template<typename T, std::size_t n>
Vector<T, n> operator-(const Vector<T, n>& vec);

// Return a zero vector; caller may have to assist compiler with type deduction by specifying type and size in angle brackets(i.e.Zero<int, 3>())s
template<typename T, std::size_t n>
Vector<T, n> Zero();

// Length squared of vec
template<typename T, std::size_t n>
T LengthSq(const Vector<T, n>& vec);
// Length of vec
template<typename T, std::size_t n>
T Length(const Vector<T, n>& vec);
// Distance squared between vecs lhs and rhs
template<typename T, std::size_t n>
T DistSq(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Distance between vecs lhs and rhs
template<typename T, std::size_t n>
T Dist(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Normalize a copy of vec
template<typename T, std::size_t n>
Vector<T, n> Normalize(const Vector<T, n>& vec);
// Dot product
template<typename T, std::size_t n>
T Dot(const Vector<T, n>& lhs, const Vector<T, n>& rhs);
// Cross product
template<typename T>
Vector<T, 3> Cross(const Vector<T, 3>& lhs, const Vector<T, 3>& rhs);
// Lerp from a to b by f
template<typename T, std::size_t n>
Vector<T, n> Lerp(const Vector<T, n>& a, const Vector<T, n>& b, float f);
// Component-wise clamp values between 0 and 1 a copy of vec
template<typename T, std::size_t n>
Vector<T, n> Saturate(const Vector<T, n>& vec);
// Component-wise clamp values between min and max a copy of vec
template<typename T, std::size_t n>
Vector<T, n> Clamp(const Vector<T, n>& vec, const T& min, const T& max);
// Component-wise absolute value a copy of vec
template<typename T, std::size_t n>
Vector<T, n> Abs(const Vector<T, n>& vec);

// Common aliases
using float2 = Vector<float, 2>;
using float3 = Vector<float, 3>;
using float4 = Vector<float, 4>;
using int2 = Vector<int, 2>;
using int3 = Vector<int, 3>;
using int4 = Vector<int, 4>;
using double2 = Vector<double, 2>;
using double3 = Vector<double, 3>;
using double4 = Vector<double, 4>;

// Base vector without size templated to avoid code-bloated binaries due to vectors of the same type with different sized loops
// See Scott Meyers' Effective C++ Item 44
namespace interior
{
	template<typename T>
	struct SizedVectorBase
	{
	public:
		static_assert(std::is_arithmetic<T>::value, "Vectors only accept arithmetic template arguments");

		T& operator[](std::size_t index);
		const T& operator[](std::size_t index) const;

		// Scalar *=
		template<typename S>
		SizedVectorBase<T>& operator*=(const S& scalar);
		// Scalar /=
		template<typename S>
		SizedVectorBase<T>& operator/=(const S& scalar);

		// Set this vector to a zero vector
		SizedVectorBase<T>& Zero();

		// Length squared of vec
		T LengthSq() const;
		// Length of vec
		T Length() const;
		// Normalize this vector in place
		void Normalize();
		// Dot product
		T Dot(const SizedVectorBase<T>& other) const;
		// Component-wise clamp values between 0 and 1 this vector in place
		void Saturate();
		// Component-wise clamp values between min and max this vector in place
		void Clamp(const T& min, const T& max);
		// Component-wise absolute value this vector in place
		void Abs();

	protected:
		constexpr SizedVectorBase(std::size_t n, T* pMem);

		constexpr void SetDataPtr(T* ptr);

		void LoopedCopyOtherRaw(const T* const other);
		void FillFromInitializerList(std::initializer_list<T> args);

		// Component-wise vector +=
		SizedVectorBase<T>& operator+=(const SizedVectorBase<T>& rhs);
		template<typename T, std::size_t n>
		friend Vector<T, n>& Vector<T, n>::operator+=(const Vector<T, n>& rhs);
		template<typename T>
		friend Vector<T, 2>& Vector<T, 2>::operator+=(const Vector<T, 2>& rhs);
		template<typename T>
		friend Vector<T, 3>& Vector<T, 3>::operator+=(const Vector<T, 3>& rhs);
		template<typename T>
		friend Vector<T, 4>& Vector<T, 4>::operator+=(const Vector<T, 4>& rhs);
		// Component-wise vector -=
		SizedVectorBase<T>& operator-=(const SizedVectorBase<T>& rhs);
		template<typename T, std::size_t n>
		friend Vector<T, n>& Vector<T, n>::operator-=(const Vector<T, n>& rhs);
		template<typename T>
		friend Vector<T, 2>& Vector<T, 2>::operator-=(const Vector<T, 2>& rhs);
		template<typename T>
		friend Vector<T, 3>& Vector<T, 3>::operator-=(const Vector<T, 3>& rhs);
		template<typename T>
		friend Vector<T, 4>& Vector<T, 4>::operator-=(const Vector<T, 4>& rhs);
		// Component-wise vector *=
		SizedVectorBase<T>& operator*=(const SizedVectorBase<T>& rhs);
		template<typename T, std::size_t n>
		friend Vector<T, n>& Vector<T, n>::operator*=(const Vector<T, n>& rhs);
		template<typename T>
		friend Vector<T, 2>& Vector<T, 2>::operator*=(const Vector<T, 2>& rhs);
		template<typename T>
		friend Vector<T, 3>& Vector<T, 3>::operator*=(const Vector<T, 3>& rhs);
		template<typename T>
		friend Vector<T, 4>& Vector<T, 4>::operator*=(const Vector<T, 4>& rhs);
		// Component-wise vector /=
		SizedVectorBase<T>& operator/=(const SizedVectorBase<T>& rhs);
		template<typename T, std::size_t n>
		friend Vector<T, n>& Vector<T, n>::operator/=(const Vector<T, n>& rhs);
		template<typename T>
		friend Vector<T, 2>& Vector<T, 2>::operator/=(const Vector<T, 2>& rhs);
		template<typename T>
		friend Vector<T, 3>& Vector<T, 3>::operator/=(const Vector<T, 3>& rhs);
		template<typename T>
		friend Vector<T, 4>& Vector<T, 4>::operator/=(const Vector<T, 4>& rhs);

	private:
		std::size_t size;
		T* pData;
	};
}

// Implementations
// Vector unspecialized implementations
template<typename T, std::size_t n>
constexpr Vector<T, n>::Vector()
	: interior::SizedVectorBase<T>(n, data.data()), data{}
{}

template<typename T, std::size_t n>
constexpr Vector<T, n>::Vector(const T& fillVal)
	: interior::SizedVectorBase<T>(n, data.data())
{
	data.fill(fillVal);
}

template<typename T, std::size_t n>
constexpr Vector<T, n>::Vector(std::initializer_list<T> args)
	: interior::SizedVectorBase<T>(n, data.data())
{
	FillFromInitializerList(args);
}

template<typename T, std::size_t n>
constexpr Vector<T, n>::Vector(const T* const rawOtherVec)
	: interior::SizedVectorBase<T>(n, data.data())
{
	LoopedCopyOtherRaw(rawOtherVec);
}

template<typename T, std::size_t n>
constexpr Vector<T, n>::Vector(const Vector<T, n>& other)
	: interior::SizedVectorBase<T>(n, data.data())
{
	data = other.data;
}

template<typename T, std::size_t n>
constexpr Vector<T, n>& Vector<T, n>::operator=(const Vector<T, n>& other)
{
	SetDataPtr(data.data());
	data = other.data;
	return *this;
}

template<typename T, std::size_t n>
Vector<T, n>& Vector<T, n>::operator+=(const Vector<T, n>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) += static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T, std::size_t n>
Vector<T, n>& Vector<T, n>::operator-=(const Vector<T, n>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) -= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T, std::size_t n>
Vector<T, n>& Vector<T, n>::operator*=(const Vector<T, n>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) *= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T, std::size_t n>
Vector<T, n>& Vector<T, n>::operator/=(const Vector<T, n>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) /= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

// Vector2 template specialization implementations
template<typename T>
constexpr Vector<T, 2>::Vector()
	: interior::SizedVectorBase<T>(2, data.data()), data{}
{}

template<typename T>
constexpr Vector<T, 2>::Vector(const T& fillVal)
	: interior::SizedVectorBase<T>(2, data.data()), data{fillVal, fillVal}
{}

template<typename T>
constexpr Vector<T, 2>::Vector(const T& inX, const T& inY)
	: interior::SizedVectorBase<T>(2, data.data()), data{inX, inY}
{}

template<typename T>
constexpr Vector<T, 2>::Vector(const T* const rawOtherVec)
	: interior::SizedVectorBase<T>(2, data.data()), data{rawOtherVec[0], rawOtherVec[1]}
{}

template<typename T>
constexpr Vector<T, 2>::Vector(const Vector<T, 2>& other)
	: interior::SizedVectorBase<T>(2, data.data()), data{other.data[0], other.data[1]}
{}

template<typename T>
constexpr Vector<T, 2>& Vector<T, 2>::operator=(const Vector<T, 2>& other)
{
	SetDataPtr(data.data());
	data = other.data;
	return *this;
}

template<typename T>
Vector<T, 2>& Vector<T, 2>::operator+=(const Vector<T, 2>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) += static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 2>& Vector<T, 2>::operator-=(const Vector<T, 2>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) -= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 2>& Vector<T, 2>::operator*=(const Vector<T, 2>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) *= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 2>& Vector<T, 2>::operator/=(const Vector<T, 2>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) /= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
const Vector<T, 2> Vector<T, 2>::zero(0, 0);
template<typename T>
const Vector<T, 2> Vector<T, 2>::unitX(1, 0);
template<typename T>
const Vector<T, 2> Vector<T, 2>::unitY(0, 1);
template<typename T>
const Vector<T, 2> Vector<T, 2>::negUnitX(-1, 0);
template<typename T>
const Vector<T, 2> Vector<T, 2>::negUnitY(0, -1);

// Vector3 template specialization implementations
template<typename T>
constexpr Vector<T, 3>::Vector()
	: interior::SizedVectorBase<T>(3, data.data()), data{}
{}

template<typename T>
constexpr Vector<T, 3>::Vector(const T& fillVal)
	: interior::SizedVectorBase<T>(3, data.data()), data{fillVal, fillVal, fillVal}
{}

template<typename T>
constexpr Vector<T, 3>::Vector(const T& inX, const T& inY, const T& inZ)
	: interior::SizedVectorBase<T>(3, data.data()), data{inX, inY, inZ}
{}

template<typename T>
constexpr Vector<T, 3>::Vector(const T* const rawOtherVec)
	: interior::SizedVectorBase<T>(3, data.data()), data{rawOtherVec[0], rawOtherVec[1], rawOtherVec[2]}
{}

template<typename T>
constexpr Vector<T, 3>::Vector(const Vector<T, 3>& other)
	: interior::SizedVectorBase<T>(3, data.data()), data{other.data[0], other.data[1], other.data[2]}
{}

template<typename T>
constexpr Vector<T, 3>& Vector<T, 3>::operator=(const Vector<T, 3>& other)
{
	SetDataPtr(data.data());
	data = other.data;
	return *this;
}

template<typename T>
Vector<T, 3> Vector<T, 3>::Cross(const Vector<T, 3>& other) const
{
	return Vector<T, 3>(data[1] * other.data[2] - data[2] * other.data[1],
		data[2] * other.data[0] - data[0] * other.data[2],
		data[0] * other.data[1] - data[1] * other.data[0]);
}

template<typename T>
Vector<T, 3>& Vector<T, 3>::operator+=(const Vector<T, 3>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) += static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 3>& Vector<T, 3>::operator-=(const Vector<T, 3>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) -= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 3>& Vector<T, 3>::operator*=(const Vector<T, 3>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) *= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 3>& Vector<T, 3>::operator/=(const Vector<T, 3>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) /= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
const Vector<T, 3> Vector<T, 3>::zero(0, 0, 0);
template<typename T>
const Vector<T, 3> Vector<T, 3>::unitX(1, 0, 0);
template<typename T>
const Vector<T, 3> Vector<T, 3>::unitY(0, 1, 0);
template<typename T>
const Vector<T, 3> Vector<T, 3>::unitZ(0, 0, 1);
template<typename T>
const Vector<T, 3> Vector<T, 3>::negUnitX(-1, 0, 0);
template<typename T>
const Vector<T, 3> Vector<T, 3>::negUnitY(0, -1, 0);
template<typename T>
const Vector<T, 3> Vector<T, 3>::negUnitZ(0, 0, -1);

// Vector4 template specialization implementations
template<typename T>
constexpr Vector<T, 4>::Vector()
	: interior::SizedVectorBase<T>(4, data.data()), data{}
{}

template<typename T>
constexpr Vector<T, 4>::Vector(const T& fillVal)
	: interior::SizedVectorBase<T>(4, data.data()), data{fillVal, fillVal, fillVal, fillVal}
{}

template<typename T>
constexpr Vector<T, 4>::Vector(const T& inX, const T& inY, const T& inZ, const T& inW)
	: interior::SizedVectorBase<T>(4, data.data()), data{inX, inY, inZ, inW}
{}

template<typename T>
constexpr Vector<T, 4>::Vector(const Vector<T, 3>& vec3, const T& scalar)
	: interior::SizedVectorBase<T>(4, data.data()), data{vec3.data[0], vec3.data[1], vec3.data[2], scalar}
{}

template<typename T>
constexpr Vector<T, 4>::Vector(const T* const rawOtherVec)
	: interior::SizedVectorBase<T>(4, data.data()), data{rawOtherVec[0], rawOtherVec[1], rawOtherVec[2], rawOtherVec[3]}
{}

template<typename T>
constexpr Vector<T, 4>::Vector(const Vector<T, 4>& other)
	: interior::SizedVectorBase<T>(4, data.data()), data{other.data[0], other.data[1], other.data[2], other.data[3]}
{}

template<typename T>
constexpr Vector<T, 4>& Vector<T, 4>::operator=(const Vector<T, 4>& other)
{
	SetDataPtr(data.data());
	data = other.data;
	return *this;
}

template<typename T>
Vector<T, 4>& Vector<T, 4>::operator+=(const Vector<T, 4>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) += static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 4>& Vector<T, 4>::operator-=(const Vector<T, 4>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) -= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 4>& Vector<T, 4>::operator*=(const Vector<T, 4>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) *= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
Vector<T, 4>& Vector<T, 4>::operator/=(const Vector<T, 4>& rhs)
{
	static_cast<interior::SizedVectorBase<T>&>(*this) /= static_cast<const interior::SizedVectorBase<T>&>(rhs);
	return *this;
}

template<typename T>
const Vector<T, 4> Vector<T, 4>::zero(0, 0, 0, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::unitX(1, 0, 0, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::unitY(0, 1, 0, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::unitZ(0, 0, 1, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::unitW(0, 0, 0, 1);
template<typename T>
const Vector<T, 4> Vector<T, 4>::negUnitX(-1, 0, 0, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::negUnitY(0, -1, 0, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::negUnitZ(0, 0, -1, 0);
template<typename T>
const Vector<T, 4> Vector<T, 4>::negUnitW(0, 0, 0, -1);

// Vector free functions
template<typename T, std::size_t n>
Vector<T, n> operator+(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> temp(lhs);
	static_cast<interior::SizedVectorBase<T>&>(temp) += rhs;
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> operator-(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> temp(lhs);
	static_cast<interior::SizedVectorBase<T>&>(temp) -= rhs;
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> operator*(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> temp(lhs);
	static_cast<interior::SizedVectorBase<T>&>(temp) *= rhs;
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> operator/(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> temp(lhs);
	static_cast<interior::SizedVectorBase<T>&>(temp) /= rhs;
	return temp;
}

template<typename T, std::size_t n, typename S>
Vector<T, n> operator* (const Vector<T, n>& vec, const S& scalar)
{
	Vector<T, n> temp(vec);
	static_cast<interior::SizedVectorBase<T>&>(temp) *= scalar;
	return temp;
}

template<typename T, std::size_t n, typename S>
Vector<T, n> operator* (const S& scalar, const Vector<T, n>& vec)
{
	Vector<T, n> temp(vec);
	static_cast<interior::SizedVectorBase<T>&>(temp) *= scalar;
	return temp;
}

template<typename T, std::size_t n, typename S>
Vector<T, n> operator/(const Vector<T, n>& vec, const S& scalar)
{
	Vector<T, n> temp(vec);
	static_cast<interior::SizedVectorBase<T>&>(temp) /= scalar;
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> operator-(const Vector<T, n>& vec)
{
	Vector<T, n> temp(vec);
	static_cast<interior::SizedVectorBase<T>&>(temp) *= -1;
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> Zero()
{
	Vector<T, n> retVec;
	return retVec;
}

template<typename T, std::size_t n>
T LengthSq(const Vector<T, n>& vec)
{
	return vec.LengthSq();
}

template<typename T, std::size_t n>
T Length(const Vector<T, n>& vec)
{
	return vec.Length();
}

template<typename T, std::size_t n>
T DistSq(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> diff(lhs - rhs);
	return diff.LengthSq();
}

template<typename T, std::size_t n>
T Dist(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	Vector<T, n> diff(lhs - rhs);
	return diff.Length();
}

template<typename T, std::size_t n>
Vector<T, n> Normalize(const Vector<T, n>& vec)
{
	Vector<T, n> temp(vec);
	temp.Normalize();
	return temp;
}

template<typename T, std::size_t n>
T Dot(const Vector<T, n>& lhs, const Vector<T, n>& rhs)
{
	return lhs.Dot(rhs);
}

template<typename T>
Vector<T, 3> Cross(const Vector<T, 3>& lhs, const Vector<T, 3>& rhs)
{
	return lhs.Cross(rhs);
}

template<typename T, std::size_t n>
Vector<T, n> Lerp(const Vector<T, n>& a, const Vector<T, n>& b, float f)
{
	return Vector<T, n>(a + f * (b - a));
}

template<typename T, std::size_t n>
Vector<T, n> Saturate(const Vector<T, n>& vec)
{
	Vector<T, n> temp(vec);
	temp.Saturate();
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> Clamp(const Vector<T, n>& vec, const T& min, const T& max)
{
	Vector<T, n> temp(vec);
	temp.Clamp(min, max);
	return temp;
}

template<typename T, std::size_t n>
Vector<T, n> Abs(const Vector<T, n>& vec)
{
	Vector<T, n> temp(vec);
	temp.Abs();
	return temp;
}

// SizedVectorBase implementations
template<typename T>
T& interior::SizedVectorBase<T>::operator[](std::size_t index)
{
	// Add const to *this's type to call const version of operator[] and then cast away const on the return
	return const_cast<T&>(static_cast<const interior::SizedVectorBase<T>&>(*this)[index]);
}

template<typename T>
const T& interior::SizedVectorBase<T>::operator[](std::size_t index) const
{
	if (index >= size)
	{
		throw std::out_of_range("Operator [] access out of bounds on Vector struct");
	}
	else
	{
		return pData[index];
	}
}

template<typename T>
template<typename S>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator*=(const S& scalar)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] *= scalar;
	}
	return *this;
}

template<typename T>
template<typename S>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator/=(const S& scalar)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] /= scalar;
	}
	return *this;
}

template<typename T>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::Zero()
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] = 0;
	}
	return *this;
}

template<typename T>
T interior::SizedVectorBase<T>::LengthSq() const
{
	T sum = 0;
	for (std::size_t i = 0; i < size; ++i)
	{
		sum += pData[i] * pData[i];
	}
	return sum;
}

template<typename T>
T interior::SizedVectorBase<T>::Length() const
{
	return sqrt(LengthSq());
}

template<typename T>
void interior::SizedVectorBase<T>::Normalize()
{
	*this /= Length();
}

template<typename T>
T interior::SizedVectorBase<T>::Dot(const interior::SizedVectorBase<T>& other) const
{
	T sum = 0;
	for (std::size_t i = 0; i < size; ++i)
	{
		sum += pData[i] * other.pData[i];
	}
	return sum;
}

template<typename T>
void interior::SizedVectorBase<T>::Saturate()
{
	Clamp(0, 1);
}

template<typename T>
void interior::SizedVectorBase<T>::Clamp(const T& min, const T& max)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] = std::max(min, std::min(pData[i], max));
	}
}

template<typename T>
void interior::SizedVectorBase<T>::Abs()
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] = abs(pData[i]);
	}
}

template<typename T>
constexpr interior::SizedVectorBase<T>::SizedVectorBase(std::size_t n, T* pMem)
	: size(n), pData(pMem)
{}

template<typename T>
constexpr void interior::SizedVectorBase<T>::SetDataPtr(T* ptr)
{
	pData = ptr;
}

template<typename T>
void interior::SizedVectorBase<T>::LoopedCopyOtherRaw(const T* const other)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] = other[i];
	}
}

template<typename T>
void interior::SizedVectorBase<T>::FillFromInitializerList(std::initializer_list<T> args)
{
	std::size_t passedInValsCount = std::min(size, args.size());
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
	for (; i < size; ++i)
	{
		pData[i] = 0;
	}
}

template<typename T>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator+=(const interior::SizedVectorBase<T>& rhs)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] += rhs.pData[i];
	}
	return *this;
}

template<typename T>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator-=(const interior::SizedVectorBase<T>& rhs)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] -= rhs.pData[i];
	}
	return *this;
}

template<typename T>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator*=(const interior::SizedVectorBase<T>& rhs)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] *= rhs.pData[i];
	}
	return *this;
}

template<typename T>
interior::SizedVectorBase<T>& interior::SizedVectorBase<T>::operator/=(const interior::SizedVectorBase<T>& rhs)
{
	for (std::size_t i = 0; i < size; ++i)
	{
		pData[i] /= rhs.pData[i];
	}
	return *this;
}