// Copyright (c) 2020-2025 - Lee HUMPHRIES (lee@md8n.com) and contributors. All rights reserved.
// Licensed under the AGPL license. See LICENSE.txt file in the project root for details.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GCodeClean.Structure;

[Flags]
public enum CoordSet
{
    None = 0b_0000_0000, // 0
    X = 0b_0000_0001, // 1
    Y = 0b_0000_0010, // 2
    Z = 0b_0000_0100, // 4
    All = X | Y | Z
}

#nullable enable

public class Coord
{
    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal Z { get; set; }

    public CoordSet Set { get; set; }

    public Coord()
    {
        X = 0M;
        Y = 0M;
        Z = 0M;
        Set = CoordSet.None;
    }

    public Coord(decimal a, decimal b, CoordSet addCoord = CoordSet.Z)
    {
        switch (addCoord)
        {
            case CoordSet.X:
                X = 0M;
                Y = a;
                Z = b;
                Set = CoordSet.Y | CoordSet.Z;
                break;
            case CoordSet.Y:
                X = a;
                Y = 0M;
                Z = b;
                Set = CoordSet.X | CoordSet.Z;
                break;
            default:
                X = a;
                Y = b;
                Z = 0M;
                Set = CoordSet.X | CoordSet.Y;
                break;
        }
    }

    public Coord(decimal x, decimal y, decimal z)
    {
        X = x;
        Y = y;
        Z = z;
        Set = CoordSet.All;
    }

    public Coord(PointF ab, CoordSet addCoord = CoordSet.Z) : this((decimal) ab.X, (decimal) ab.Y, addCoord)
    {
    }

    public Coord(Coord coord)
    {
        X = coord.X;
        Y = coord.Y;
        Z = coord.Z;
        Set = coord.Set;
    }

    public bool HasCoordPair()
    {
        var hasX = (Set & CoordSet.X) == CoordSet.X ? 1 : 0;
        var hasY = (Set & CoordSet.Y) == CoordSet.Y ? 1 : 0;
        var hasZ = (Set & CoordSet.Z) == CoordSet.Z ? 1 : 0;

        return hasX + hasY + hasZ >= 2;
    }

    public PointF ToPointF(CoordSet dropCoord = CoordSet.Z)
    {
        return dropCoord switch {
            CoordSet.X => new PointF((float)Y, (float)Z),
            CoordSet.Y => new PointF((float)X, (float)Z),
            _ => new PointF((float)X, (float)Y),
        };
    }

    /// <summary>
    /// Create a new coords object from coords 1, and then merge coords2 into it.
    /// Individual coords in coords2 that are not Set are not copied over
    /// Existing individual coords are not replaced unless overwrite is true
    /// </summary>
    public static Coord Merge(Coord coords1, Coord coords2, bool overwrite = false)
    {
        var coords3 = new Coord(coords1);

        var hasX3 = (coords3.Set & CoordSet.X) == CoordSet.X;
        var hasY3 = (coords3.Set & CoordSet.Y) == CoordSet.Y;
        var hasZ3 = (coords3.Set & CoordSet.Z) == CoordSet.Z;

        var hasX2 = (coords2.Set & CoordSet.X) == CoordSet.X;
        var hasY2 = (coords2.Set & CoordSet.Y) == CoordSet.Y;
        var hasZ2 = (coords2.Set & CoordSet.Z) == CoordSet.Z;

        if ((!hasX3 || overwrite) && hasX2)
        {
            coords3.X = coords2.X;
            coords3.Set |= CoordSet.X;
        }

        if ((!hasY3 || overwrite) && hasY2)
        {
            coords3.Y = coords2.Y;
            coords3.Set |= CoordSet.Y;
        }

        if (hasZ3 && !overwrite || !hasZ2)
        {
            return coords3;
        }

        coords3.Z = coords2.Z;
        coords3.Set |= CoordSet.Z;

        return coords3;
    }

    public static Coord Difference(Coord coords1, Coord coords2)
    {
        var coords3 = coords1 - coords2;

        return new Coord(Math.Abs(coords3.X), Math.Abs(coords3.Y), Math.Abs(coords3.Z));
    }

    /// <summary>
    /// The distance between two coords (all three axes)
    /// </summary>
    /// <param name="coords1"></param>
    /// <param name="coords2"></param>
    /// <returns></returns>
    public static decimal Distance(Coord coords1, Coord coords2)
    {
        var a = Math.Abs(coords1.X - coords2.X);
        var b = Math.Abs(coords1.Y - coords2.Y);
        var c = Math.Abs(coords1.Z - coords2.Z);

        var ab = Math.Sqrt((Double)((a * a) + (b * b)));
        var abc = Math.Sqrt((ab * ab) + (Double)(c * c));

        return (decimal)abc;
    }

    public static Coord operator -(Coord coords1, Coord coords2)
    {
        var coords3 = new Coord(coords2.X - coords1.X, coords2.Y - coords1.Y, coords2.Z - coords1.Z)
        {
            Set = coords1.Set | coords2.Set
        };

        return coords3;
    }

    public static Coord operator +(Coord coords1, Coord coords2)
    {
        var coords3 = new Coord(coords2.X + coords1.X, coords2.Y + coords1.Y, coords2.Z + coords1.Z)
        {
            Set = coords1.Set | coords2.Set
        };

        return coords3;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Coord other) return false;
        return X == other.X && Y == other.Y && Z == other.Z && Set == other.Set;
    }

    public override int GetHashCode() {
        return HashCode.Combine(X, Y, Z, Set);
    }

    public static bool operator ==(Coord? left, Coord? right) {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Coord? left, Coord? right) {
        return !(left == right);
    }

    /// <summary>
    /// Determines if all supplied coords are in the same orthogonal plane (X, Y or Z)
    /// </summary>
    /// <return>
    /// boolean - false if no coords are supplied, true for 1 coord, true or false for 2 or more coords
    /// </return>
    public static bool Coplanar(List<Coord> coords)
    {
        if (coords.Count <= 1)
        {
            return coords.Count > 0;
        }

        var allXSame = coords.Select(c => c.X).Distinct().Count() == 1;
        var allYSame = coords.Select(c => c.Y).Distinct().Count() == 1;
        var allZSame = coords.Select(c => c.Z).Distinct().Count() == 1;

        return allXSame || allYSame || allZSame;
    }

    /// <summary>
    /// Determines which orthogonal plane (X, Y or Z) if any, that is shared by the coords
    /// </summary>
    /// <return>
    /// string - "" if no coords are supplied, "XYZ" for 1 coords, relevant value for 2 or more coords
    /// </return>
    public static CoordSet Ortho(List<Coord> coords)
    {
        if (coords.Count < 2)
        {
            return coords.Count == 0 ? CoordSet.None : CoordSet.All;
        }

        var allX = coords.Select(c => c.X).Distinct().Count() == 1 ? CoordSet.X : CoordSet.None;
        var allY = coords.Select(c => c.Y).Distinct().Count() == 1 ? CoordSet.Y : CoordSet.None;
        var allZ = coords.Select(c => c.Z).Distinct().Count() == 1 ? CoordSet.Z : CoordSet.None;

        return allX | allY | allZ;
    }

    public override string ToString() {
        var tokens = new List<Token>();

        if ((Set & CoordSet.X) == CoordSet.X) {
            tokens.Add(new Token($"X{X}"));
        }

        if ((Set & CoordSet.Y) == CoordSet.Y) {
            tokens.Add(new Token($"Y{Y}"));
        }

        if ((Set & CoordSet.Z) == CoordSet.Z) {
            tokens.Add(new Token($"Z{Z}"));
        }

        return string.Join(' ', tokens);
    }

    public string ToXYCoord() => $"X{X}Y{Y}";
}