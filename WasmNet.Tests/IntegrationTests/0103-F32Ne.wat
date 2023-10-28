;; invoke: ne
;; expect: (i32:1)

(module
    (func (export "ne") (result i32)
        f32.const -42.0
        f32.const 2.1
        f32.ne
    )
)