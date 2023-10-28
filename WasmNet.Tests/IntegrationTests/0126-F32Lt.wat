;; invoke: lt
;; expect: (i32:1)

(module
    (func (export "lt") (result i32)
        f32.const -42.0
        f32.const 2.1
        f32.lt
    )
)