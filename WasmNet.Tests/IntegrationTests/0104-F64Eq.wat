;; invoke: eq
;; expect: (i32:0)

(module
    (func (export "eq") (result i32)
        f64.const -42.0
        f64.const 2.1
        f64.eq
    )
)