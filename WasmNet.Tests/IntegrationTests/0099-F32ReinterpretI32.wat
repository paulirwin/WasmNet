;; invoke: reinterpret
;; expect: (f32:42)

(module
    (func (export "reinterpret") (result f32)
        i32.const 1109917696
        f32.reinterpret_i32
    )
)