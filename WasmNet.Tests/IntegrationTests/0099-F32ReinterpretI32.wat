;; invoke: reinterpret
;; expect: (f32:42)

(module
    (memory 1)
    (func (export "reinterpret") (result f32)
        i32.const 1109917696
        f32.reinterpret_i32
    )
)