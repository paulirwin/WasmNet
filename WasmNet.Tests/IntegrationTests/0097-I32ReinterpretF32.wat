;; invoke: reinterpret
;; expect: (i32:1109917696)

(module
    (func (export "reinterpret") (result i32)
        f32.const 42.0
        i32.reinterpret_f32
    )
)