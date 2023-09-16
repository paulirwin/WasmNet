;; invoke: eq
;; expect: (i32:0)

(module
    (func (export "eq") (result i32)
        i32.const -42
        i32.const 2
        i32.eq
    )
)