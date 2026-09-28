# 09: Cover duplicate predicate registration

**What to build:** Verify that `PredicateRegistryBuilder` rejects registering two predicates under the same name (including case-insensitive collisions), for both the class-based `Add<TPredicate>()` overload and the lambda `Add(schema, evaluate)` overload — an easy authoring mistake with no current regression coverage.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Registering two class-based predicates (`Add<TPredicate>()`) under the same name throws `ArgumentException` with an "already registered" message.
- [ ] Registering two lambda predicates (`Add(schema, evaluate)`) under the same name throws the same way.
- [ ] Registering the same name with different casing (e.g. `"Foo"` then `"foo"`) is treated as a collision and throws.
- [ ] Existing registry-building tests continue to pass unchanged.
