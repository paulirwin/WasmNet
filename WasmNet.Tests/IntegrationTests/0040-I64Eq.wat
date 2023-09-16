;; invoke: eq
;; expect: (i32:1)

(module
    (func (export "eq") (result i32)
        i64.const 2
        i64.const 2
        i64.eq
    )
)