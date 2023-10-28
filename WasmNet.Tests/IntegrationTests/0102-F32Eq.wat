;; invoke: eq
;; expect: (i32:0)

(module
    (func (export "eq") (result i32)
        f32.const -42.0
        f32.const 2.1
        f32.eq
    )
)