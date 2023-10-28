;; invoke: gt
;; expect: (i32:0)

(module
    (func (export "gt") (result i32)
        f32.const -42.0
        f32.const 2.1
        f32.gt
    )
)