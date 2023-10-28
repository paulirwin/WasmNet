;; invoke: le
;; expect: (i32:1)

(module
    (func (export "le") (result i32)
        f32.const -42.0
        f32.const -42.0
        f32.le
    )
)