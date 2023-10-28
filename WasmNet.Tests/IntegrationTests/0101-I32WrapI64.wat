;; invoke: wrap
;; expect: (i32:-94967296)

(module
    (func (export "wrap") (result i32)
        i64.const 4200000000
        i32.wrap_i64
    )
)